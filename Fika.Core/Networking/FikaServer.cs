﻿// © 2024 Lacyway All Rights Reserved

using Aki.Common.Http;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.AssetsManager;
using EFT.Interactive;
using EFT.UI;
using LiteNetLib;
using LiteNetLib.Utils;
using Fika.Core.Coop.Components;
using Fika.Core.Coop.GameMode;
using Fika.Core.Coop.Matchmaker;
using Fika.Core.Coop.Models;
using Fika.Core.Coop.Players;
using Fika.Core.Modding;
using Fika.Core.Modding.Events;
using Fika.Core.Networking.Packets.GameWorld;
using Open.Nat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Fika.Core.Networking
{
    public class FikaServer : MonoBehaviour, INetEventListener, INetLogger
    {
        private NetManager _netServer;
        public NetPacketProcessor packetProcessor = new();
        private NetDataWriter _dataWriter = new();
        public CoopPlayer MyPlayer => (CoopPlayer)Singleton<GameWorld>.Instance.MainPlayer;
        public Dictionary<string, CoopPlayer> Players => CoopHandler.Players;
        public List<string> PlayersMissing = [];
        public string MyExternalIP { get; private set; } = NetUtils.GetLocalIp(LocalAddrType.IPv4);
        private int Port => FikaPlugin.UDPPort.Value;
        private CoopHandler CoopHandler { get; set; }
        public int ReadyClients = 0;
        public NetManager NetServer
        {
            get
            {
                return _netServer;
            }
        }
        public DateTime timeSinceLastPeerDisconnected = DateTime.Now.AddDays(1);
        public bool hasHadPeer = false;
        private ManualLogSource serverLogger;
        public bool ServerReady = false;

        public async void Start()
        {
            NetDebug.Logger = this;
            serverLogger = new("Fika Server");

            packetProcessor.SubscribeNetSerializable<PlayerStatePacket, NetPeer>(OnPlayerStatePacketReceived);
            packetProcessor.SubscribeNetSerializable<GameTimerPacket, NetPeer>(OnGameTimerPacketReceived);
            packetProcessor.SubscribeNetSerializable<WeaponPacket, NetPeer>(OnFirearmPacketReceived);
            packetProcessor.SubscribeNetSerializable<DamagePacket, NetPeer>(OnDamagePacketReceived);
            packetProcessor.SubscribeNetSerializable<InventoryPacket, NetPeer>(OnInventoryPacketReceived);
            packetProcessor.SubscribeNetSerializable<CommonPlayerPacket, NetPeer>(OnCommonPlayerPacketReceived);
            packetProcessor.SubscribeNetSerializable<AllCharacterRequestPacket, NetPeer>(OnAllCharacterRequestPacketReceived);
            packetProcessor.SubscribeNetSerializable<InformationPacket, NetPeer>(OnInformationPacketReceived);
            packetProcessor.SubscribeNetSerializable<HealthSyncPacket, NetPeer>(OnHealthSyncPacketReceived);
            packetProcessor.SubscribeNetSerializable<GenericPacket, NetPeer>(OnGenericPacketReceived);
            packetProcessor.SubscribeNetSerializable<ExfiltrationPacket, NetPeer>(OnExfiltrationPacketReceived);
            packetProcessor.SubscribeNetSerializable<WeatherPacket, NetPeer>(OnWeatherPacketReceived);
            packetProcessor.SubscribeNetSerializable<BTRInteractionPacket, NetPeer>(OnBTRInteractionPacketReceived);
            packetProcessor.SubscribeNetSerializable<BTRServicePacket, NetPeer>(OnBTRServicePacketReceived);
            packetProcessor.SubscribeNetSerializable<DeathPacket, NetPeer>(OnDeathPacketReceived);
            packetProcessor.SubscribeNetSerializable<MinePacket, NetPeer>(OnMinePacketReceived);
            packetProcessor.SubscribeNetSerializable<BorderZonePacket, NetPeer>(OnBorderZonePacketReceived);
            packetProcessor.SubscribeNetSerializable<SendCharacterPacket, NetPeer>(OnSendCharacterPacketReceived);

            _netServer = new NetManager(this)
            {
                BroadcastReceiveEnabled = true,
                UpdateTime = 15,
                AutoRecycle = true,
                IPv6Enabled = false,
                DisconnectTimeout = 15000,
                UseNativeSockets = FikaPlugin.NativeSockets.Value,
                EnableStatistics = true
            };

            if (FikaPlugin.UseUPnP.Value)
            {
                bool upnpFailed = false;

                await Task.Run(async () =>
                {
                    try
                    {
                        NatDiscoverer discoverer = new();
                        CancellationTokenSource cts = new(10000);
                        NatDevice device = await discoverer.DiscoverDeviceAsync(PortMapper.Upnp, cts);
                        IPAddress extIp = await device.GetExternalIPAsync();
                        MyExternalIP = extIp.MapToIPv4().ToString();

                        await device.CreatePortMapAsync(new Mapping(Protocol.Udp, Port, Port, 300, "Fika UDP"));
                    }
                    catch (Exception ex)
                    {
                        serverLogger.LogError($"Error when attempting to map UPnP. Make sure the selected port is not already open! Error message: {ex.Message}");
                        upnpFailed = true;
                    }
                });

                if (upnpFailed)
                {
                    Singleton<PreloaderUI>.Instance.ShowErrorScreen("Network Error", "UPnP mapping failed. Make sure the selected port is not already open!");
                }
            }
            else if (FikaPlugin.ForceIP.Value != "")
            {
                MyExternalIP = FikaPlugin.ForceIP.Value;
                NotificationManagerClass.DisplayMessageNotification($"Force IP enabled and set to: {MyExternalIP}", iconType: EFT.Communications.ENotificationIconType.Alert);
            }
            else
            {
                try
                {
                    HttpClient client = new();
                    string ipAdress = await client.GetStringAsync("https://ipv4.icanhazip.com/");
                    MyExternalIP = ipAdress.Replace("\n", "");
                    client.Dispose();
                }
                catch (Exception)
                {
                    Singleton<PreloaderUI>.Instance.ShowErrorScreen("Network Error", "Error when trying to receive IP automatically.");
                }
            }

            if (FikaPlugin.ForceBindIP.Value != "")
            {
                _netServer.Start(FikaPlugin.ForceBindIP.Value, "", Port);
            }
            else
            {
                _netServer.Start(Port);
            }

            serverLogger.LogInfo("Started Fika Server");
            NotificationManagerClass.DisplayMessageNotification($"Server started on port {_netServer.LocalPort}.",
                EFT.Communications.ENotificationDurationType.Default, EFT.Communications.ENotificationIconType.EntryPoint);

            string body = new SetHostRequest(MyExternalIP, Port).ToJson();
            RequestHandler.PostJson("/fika/update/sethost", body);

            Singleton<FikaServer>.Create(this);
            FikaEventDispatcher.DispatchEvent(new FikaServerCreatedEvent(this));
            ServerReady = true;
        }

        private void OnSendCharacterPacketReceived(SendCharacterPacket packet, NetPeer peer)
        {
            if (packet.PlayerInfo.Profile.ProfileId != MyPlayer.ProfileId)
            {
                CoopHandler.QueueProfile(packet.PlayerInfo.Profile, packet.Position, packet.IsAlive, packet.IsAI);
            }

            _dataWriter.Reset();
            SendDataToAll(_dataWriter, ref packet, DeliveryMethod.ReliableUnordered, peer);
        }

        private void OnBorderZonePacketReceived(BorderZonePacket packet, NetPeer peer)
        {
            // This shouldn't happen
        }

        private void OnMinePacketReceived(MinePacket packet, NetPeer peer)
        {
            if (Singleton<GameWorld>.Instance.MineManager != null)
            {
                NetworkGame.Class1381 mineSeeker = new()
                {
                    minePosition = packet.MinePositon
                };
                MineDirectional mineDirectional = Singleton<GameWorld>.Instance.MineManager.Mines.FirstOrDefault(new Func<MineDirectional, bool>(mineSeeker.method_0));
                if (mineDirectional == null)
                {
                    return;
                }
                mineDirectional.Explosion();
            }
        }

        private void OnDeathPacketReceived(DeathPacket packet, NetPeer peer)
        {
            if (Players.TryGetValue(packet.ProfileId, out CoopPlayer playerToApply))
            {
                playerToApply.HandleDeathPatchet(packet);
            }

            _dataWriter.Reset();
            SendDataToAll(_dataWriter, ref packet, DeliveryMethod.ReliableOrdered, peer);
        }

        private void OnBTRServicePacketReceived(BTRServicePacket packet, NetPeer peer)
        {
            CoopHandler.serverBTR?.NetworkBtrTraderServicePurchased(packet);
        }

        private void OnBTRInteractionPacketReceived(BTRInteractionPacket packet, NetPeer peer)
        {
            if (CoopHandler.serverBTR != null)
            {
                if (Players.TryGetValue(packet.ProfileId, out CoopPlayer player))
                {
                    if (CoopHandler.serverBTR.CanPlayerEnter(player))
                    {
                        CoopHandler.serverBTR.HostObservedInteraction(player, packet.InteractPacket);

                        _dataWriter.Reset();
                        SendDataToAll(_dataWriter, ref packet, DeliveryMethod.ReliableOrdered);
                    }
                    else
                    {
                        BTRInteractionPacket newPacket = new(packet.ProfileId)
                        {
                            HasInteractPacket = false
                        };

                        _dataWriter.Reset();
                        SendDataToAll(_dataWriter, ref newPacket, DeliveryMethod.ReliableOrdered);
                    }
                }
            }
        }

        private void OnWeatherPacketReceived(WeatherPacket packet, NetPeer peer)
        {
            if (packet.IsRequest)
            {
                if (MatchmakerAcceptPatches.Nodes != null)
                {
                    WeatherPacket weatherPacket2 = new()
                    {
                        IsRequest = false,
                        HasData = true,
                        Amount = MatchmakerAcceptPatches.Nodes.Length,
                        WeatherClasses = MatchmakerAcceptPatches.Nodes
                    };
                    _dataWriter.Reset();
                    SendDataToPeer(peer, _dataWriter, ref weatherPacket2, DeliveryMethod.ReliableOrdered);
                };
            }
        }

        private void OnExfiltrationPacketReceived(ExfiltrationPacket packet, NetPeer peer)
        {
            if (packet.IsRequest)
            {
                if (ExfiltrationControllerClass.Instance != null)
                {
                    ExfiltrationControllerClass exfilController = ExfiltrationControllerClass.Instance;

                    if (exfilController.ExfiltrationPoints == null)
                        return;

                    ExfiltrationPacket exfilPacket = new(false)
                    {
                        ExfiltrationAmount = exfilController.ExfiltrationPoints.Length,
                        ExfiltrationPoints = []
                    };

                    foreach (ExfiltrationPoint exfilPoint in exfilController.ExfiltrationPoints)
                    {
                        exfilPacket.ExfiltrationPoints.Add(exfilPoint.Settings.Name, exfilPoint.Status);
                    }

                    if (MyPlayer.Side == EPlayerSide.Savage && exfilController.ScavExfiltrationPoints != null)
                    {
                        exfilPacket.HasScavExfils = true;
                        exfilPacket.ScavExfiltrationAmount = exfilController.ScavExfiltrationPoints.Length;
                        exfilPacket.ScavExfiltrationPoints = [];

                        foreach (ScavExfiltrationPoint scavExfilPoint in exfilController.ScavExfiltrationPoints)
                        {
                            exfilPacket.ScavExfiltrationPoints.Add(scavExfilPoint.Settings.Name, scavExfilPoint.Status);
                        }
                    }

                    _dataWriter.Reset();
                    SendDataToPeer(peer, _dataWriter, ref exfilPacket, DeliveryMethod.ReliableOrdered);
                }
                else
                {
                    serverLogger.LogError($"ExfiltrationPacketPacketReceived: ExfiltrationController was null");
                }
            }
        }

        private void OnGenericPacketReceived(GenericPacket packet, NetPeer peer)
        {
            if (!Players.ContainsKey(packet.ProfileId))
            {
                return;
            }

            if (packet.PacketType == EPackageType.ClientExtract)
            {
                if (CoopHandler.Players.TryGetValue(packet.ProfileId, out CoopPlayer playerToApply))
                {
                    CoopHandler.Players.Remove(packet.ProfileId);
                    if (!CoopHandler.ExtractedPlayers.Contains(packet.ProfileId))
                    {
                        CoopHandler.ExtractedPlayers.Add(packet.ProfileId);
                        CoopGame coopGame = (CoopGame)CoopHandler.LocalGameInstance;
                        coopGame.ExtractedPlayers.Add(packet.ProfileId);
                        coopGame.ClearHostAI(playerToApply);

                        if (FikaPlugin.ShowNotifications.Value)
                        {
                            NotificationManagerClass.DisplayMessageNotification($"Group member '{playerToApply.Profile.Nickname}' has extracted.",
                                            EFT.Communications.ENotificationDurationType.Default, EFT.Communications.ENotificationIconType.EntryPoint);
                        }
                    }

                    playerToApply.Dispose();
                    AssetPoolObject.ReturnToPool(playerToApply.gameObject, true);
                }
            }
            else if (packet.PacketType == EPackageType.Ping && FikaPlugin.UsePingSystem.Value)
            {
                if (Players.TryGetValue(packet.ProfileId, out CoopPlayer playerToApply))
                {
                    playerToApply.ReceivePing(packet.PingLocation, packet.PingType, packet.PingColor, packet.Nickname);
                }
            }
            else if (packet.PacketType == EPackageType.LoadBot)
            {
                if (Players.TryGetValue(packet.ProfileId, out CoopPlayer playerToApply))
                {
                    if (playerToApply is CoopBot botToApply)
                    {
                        botToApply.loadedPlayers++;
                    }
                }

                return;
            }
            else if (packet.PacketType == EPackageType.ExfilCountdown)
            {
                if (ExfiltrationControllerClass.Instance != null)
                {
                    ExfiltrationControllerClass exfilController = ExfiltrationControllerClass.Instance;

                    ExfiltrationPoint exfilPoint = exfilController.ExfiltrationPoints.FirstOrDefault(x => x.Settings.Name == packet.ExfilName);
                    if (exfilPoint != null)
                    {
                        CoopGame game = (CoopGame)Singleton<AbstractGame>.Instance;
                        exfilPoint.ExfiltrationStartTime = game != null ? game.PastTime : packet.ExfilStartTime;

                        if (exfilPoint.Status != EExfiltrationStatus.Countdown)
                        {
                            exfilPoint.Status = EExfiltrationStatus.Countdown;
                        }
                    }
                }
            }
            _dataWriter.Reset();
            SendDataToAll(_dataWriter, ref packet, DeliveryMethod.ReliableOrdered, peer);
        }

        private void OnHealthSyncPacketReceived(HealthSyncPacket packet, NetPeer peer)
        {
            if (Players.TryGetValue(packet.ProfileId, out CoopPlayer playerToApply))
            {
                playerToApply?.PacketReceiver?.HealthSyncPackets?.Enqueue(packet);
            }

            _dataWriter.Reset();
            SendDataToAll(_dataWriter, ref packet, DeliveryMethod.ReliableOrdered, peer);
        }

        private void OnInformationPacketReceived(InformationPacket packet, NetPeer peer)
        {
            ReadyClients += packet.ReadyPlayers;

            CoopGame coopGame = (CoopGame)Singleton<IFikaGame>.Instance;

            InformationPacket respondPackage = new(false)
            {
                NumberOfPlayers = _netServer.ConnectedPeersCount,
                ReadyPlayers = ReadyClients,
                ForceStart = coopGame.forceStart
            };

            _dataWriter.Reset();
            SendDataToAll(_dataWriter, ref respondPackage, DeliveryMethod.ReliableOrdered);
        }

        private void OnAllCharacterRequestPacketReceived(AllCharacterRequestPacket packet, NetPeer peer)
        {
            // This method needs to be refined. For some reason the ping-pong has to be run twice for it to work on the host?
            if (packet.IsRequest)
            {
                foreach (CoopPlayer player in CoopHandler.Players.Values)
                {
                    if (player.ProfileId == packet.ProfileId)
                    {
                        continue;
                    }

                    if (packet.Characters.Contains(player.ProfileId))
                    {
                        continue;
                    }

                    AllCharacterRequestPacket requestPacket = new(player.ProfileId)
                    {
                        IsRequest = false,
                        PlayerInfo = new()
                        {
                            Profile = player.Profile
                        },
                        IsAlive = player.HealthController.IsAlive,
                        IsAI = player is CoopBot,
                        Position = player.Transform.position
                    };
                    _dataWriter.Reset();
                    SendDataToPeer(peer, _dataWriter, ref requestPacket, DeliveryMethod.ReliableOrdered);
                }
            }
            if (!Players.ContainsKey(packet.ProfileId) && !PlayersMissing.Contains(packet.ProfileId) && !CoopHandler.ExtractedPlayers.Contains(packet.ProfileId))
            {
                PlayersMissing.Add(packet.ProfileId);
                serverLogger.LogInfo($"Requesting missing player from server.");
                AllCharacterRequestPacket requestPacket = new(MyPlayer.ProfileId);
                _dataWriter.Reset();
                SendDataToPeer(peer, _dataWriter, ref requestPacket, DeliveryMethod.ReliableOrdered);
            }
            if (!packet.IsRequest && PlayersMissing.Contains(packet.ProfileId))
            {
                serverLogger.LogInfo($"Received CharacterRequest from client: ProfileID: {packet.PlayerInfo.Profile.ProfileId}, Nickname: {packet.PlayerInfo.Profile.Nickname}");
                if (packet.ProfileId != MyPlayer.ProfileId)
                {
                    CoopHandler.QueueProfile(packet.PlayerInfo.Profile, new Vector3(packet.Position.x, packet.Position.y + 0.5f, packet.Position.y), packet.IsAlive);
                    PlayersMissing.Remove(packet.ProfileId);
                }
            }
        }

        private void OnCommonPlayerPacketReceived(CommonPlayerPacket packet, NetPeer peer)
        {
            if (Players.TryGetValue(packet.ProfileId, out CoopPlayer playerToApply))
            {
                playerToApply?.PacketReceiver?.CommonPlayerPackets?.Enqueue(packet);
            }

            _dataWriter.Reset();
            SendDataToAll(_dataWriter, ref packet, DeliveryMethod.ReliableOrdered, peer);
        }

        private void OnInventoryPacketReceived(InventoryPacket packet, NetPeer peer)
        {
            if (Players.TryGetValue(packet.ProfileId, out CoopPlayer playerToApply))
            {
                playerToApply?.PacketReceiver?.InventoryPackets?.Enqueue(packet);
            }

            _dataWriter.Reset();
            SendDataToAll(_dataWriter, ref packet, DeliveryMethod.ReliableOrdered, peer);
        }

        private void OnDamagePacketReceived(DamagePacket packet, NetPeer peer)
        {
            if (Players.TryGetValue(packet.ProfileId, out CoopPlayer playerToApply))
            {
                playerToApply?.PacketReceiver?.DamagePackets?.Enqueue(packet);
            }

            _dataWriter.Reset();
            SendDataToAll(_dataWriter, ref packet, DeliveryMethod.ReliableOrdered, peer);
        }

        private void OnFirearmPacketReceived(WeaponPacket packet, NetPeer peer)
        {
            if (Players.TryGetValue(packet.ProfileId, out CoopPlayer playerToApply))
            {
                playerToApply?.PacketReceiver?.FirearmPackets?.Enqueue(packet);
            }

            _dataWriter.Reset();
            SendDataToAll(_dataWriter, ref packet, DeliveryMethod.ReliableOrdered, peer);
        }

        private void OnGameTimerPacketReceived(GameTimerPacket packet, NetPeer peer)
        {
            if (!packet.IsRequest)
                return;

            CoopGame game = (CoopGame)Singleton<AbstractGame>.Instance;
            if (game != null)
            {
                GameTimerPacket gameTimerPacket = new(false, (game.GameTimer.SessionTime - game.GameTimer.PastTime).Value.Ticks);
                _dataWriter.Reset();
                SendDataToPeer(peer, _dataWriter, ref gameTimerPacket, DeliveryMethod.ReliableOrdered);
            }
            else
            {
                serverLogger.LogError("OnGameTimerPacketReceived: Game was null!");
            }
        }

        private void OnPlayerStatePacketReceived(PlayerStatePacket packet, NetPeer peer)
        {
            if (Players.TryGetValue(packet.ProfileId, out CoopPlayer playerToApply))
            {
                playerToApply.PacketReceiver.NewState = packet;
            }

            _dataWriter.Reset();
            SendDataToAll(_dataWriter, ref packet, DeliveryMethod.ReliableOrdered, peer);
        }

        protected void Awake()
        {
            CoopHandler = CoopHandler.CoopHandlerParent.GetComponent<CoopHandler>();
        }

        void Update()
        {
            _netServer.PollEvents();
        }

        void OnDestroy()
        {
            NetDebug.Logger = null;
            _netServer?.Stop();

            FikaEventDispatcher.DispatchEvent(new FikaServerDestroyedEvent(this));
        }

        public void SendDataToAll<T>(NetDataWriter writer, ref T packet, DeliveryMethod deliveryMethod, NetPeer peerToExclude = null) where T : INetSerializable
        {

            if (peerToExclude != null)
            {
                if (NetServer.ConnectedPeersCount > 1)
                {
                    packetProcessor.WriteNetSerializable(writer, ref packet);
                    _netServer.SendToAll(writer, deliveryMethod, peerToExclude);
                }
            }
            else
            {
                packetProcessor.WriteNetSerializable(writer, ref packet);
                _netServer.SendToAll(writer, deliveryMethod);
            }
        }

        public void SendDataToPeer<T>(NetPeer peer, NetDataWriter writer, ref T packet, DeliveryMethod deliveryMethod) where T : INetSerializable
        {
            packetProcessor.WriteNetSerializable(writer, ref packet);
            peer.Send(writer, deliveryMethod);
        }

        public void OnPeerConnected(NetPeer peer)
        {
            NotificationManagerClass.DisplayMessageNotification($"Peer connected to server on port {peer.Port}.", iconType: EFT.Communications.ENotificationIconType.Friend);

            hasHadPeer = true;
        }

        public void OnNetworkError(IPEndPoint endPoint, SocketError socketErrorCode)
        {
            serverLogger.LogError("[SERVER] error " + socketErrorCode);
        }

        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
            if (messageType == UnconnectedMessageType.Broadcast)
            {
                serverLogger.LogInfo("[SERVER] Received discovery request. Send discovery response");
                NetDataWriter resp = new();
                resp.Put(1);
                _netServer.SendUnconnectedMessage(resp, remoteEndPoint);
            }
        }

        public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
        }

        public void OnConnectionRequest(ConnectionRequest request)
        {
            request.AcceptIfKey("fika.core");
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            serverLogger.LogInfo("Peer disconnected " + peer.Port + ", info: " + disconnectInfo.Reason);
            NotificationManagerClass.DisplayMessageNotification("Peer disconnected " + peer.Port + ", info: " + disconnectInfo.Reason, iconType: EFT.Communications.ENotificationIconType.Alert);
            if (_netServer.ConnectedPeersCount == 0)
            {
                timeSinceLastPeerDisconnected = DateTime.Now;
            }
        }

        public void WriteNet(NetLogLevel level, string str, params object[] args)
        {
            Debug.LogFormat(str, args);
        }

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
        {
            packetProcessor.ReadAllPackets(reader, peer);
        }
    }
}
