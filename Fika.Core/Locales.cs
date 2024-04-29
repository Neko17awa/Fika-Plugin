using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fika.Core
{
    internal partial class Locales
    {
        private static Dictionary<string, Dictionary<string, string>> translations = new Dictionary<string, Dictionary<string, string>>()
{
    {"中文（简体）", new Dictionary<string, string>{
        {"Show_Feed", "显示反馈"},
        {"Auto_Extract", "自动撤离"},
        {"Show_Extract_Message", "显示撤离消息"},
        {"Faster_Inventory_Scroll", "更快的库存滚动"},
        {"Faster_Inventory_Scroll_Speed", "更快的库存滚动速度"},
        {"Show_Player_Name_Plates", "显示玩家名牌"},
        {"Show_HP_Percentage_Instead_of_Bar", "显示百分比的血量而不是条形图"},
        {"Show_Player_Faction_Icon", "显示玩家阵营图标"},
        {"Name_Plate_Scale", "名牌比例"},
        {"Ping_System", "标记系统"},
        {"Ping_Button", "标记按钮"},
        {"Ping_Color", "标记颜色"},
        {"Ping_Size", "标记大小"},
        {"Play_Ping_Animation", "播放标记动画"},
        {"Free_Camera_Button", "自由相机按钮"},
        {"Dynamic_AI", "动态AI"},
        {"Dynamic_AI_Range", "动态AI范围"},
        {"Dynamic_AI_Rate", "动态AI速率"},
        {"Culling_System", "剔除系统"},
        {"Culling_Range", "剔除范围"},
        {"Enforced_Spawn_Limits", "强制生成限制"},
        {"Despawn_Furthest", "最远消失"},
        {"Max_Bots_Factory", "工厂最大机器人数"},
        {"Max_Bots_Customs", "海关最大机器人数"},
        {"Max_Bots_Interchange", "交换处最大机器人数"},
        {"Max_Bots_Reserve", "储备站最大机器人数"},
        {"Max_Bots_Woods", "森林最大机器人数"},
        {"Max_Bots_Shoreline", "海岸线最大机器人数"},
        {"Max_Bots_Streets", "街区最大机器人数"},
        {"Max_Bots_Ground_Zero", "中心区最大机器人数"},
        {"Max_Bots_Labs", "实验室最大机器人数"},
        {"Max_Bots_Lighthouse", "灯塔最大机器人数"},
        {"Native_Sockets", "原生Sockets"},
        {"Force_IP", "强制IP"},
        {"Force_Bind_IP", "强制绑定IP"},
        {"Auto_Server_Refresh_Rate", "自动服务器刷新率"},
        {"UDP_Port", "UDP端口"},
        {"Use_UPnP", "使用UPnP"},
        {"Head_Damage_Multiplier", "头部伤害倍率"},
        {"Armpit_Damage_Multiplier", "腋窝伤害倍率"},
    }},
    {"English", new Dictionary<string, string>{
        {"Show_Feed", "Show Feed"},
        {"Auto_Extract", "Auto Extract"},
        {"Show_Extract_Message", "Show Extract Message"},
        {"Faster_Inventory_Scroll", "Faster Inventory Scroll"},
        {"Faster_Inventory_Scroll_Speed", "Faster Inventory Scroll Speed"},
        {"Show_Player_Name_Plates", "Show Player Name Plates"},
        {"Show_HP_Percentage_Instead_of_Bar", "Show HP Percentage Instead of Bar"},
        {"Show_Player_Faction_Icon", "Show Player Faction Icon"},
        {"Name_Plate_Scale", "Name Plate Scale"},
        {"Ping_System", "Ping System"},
        {"Ping_Button", "Ping Button"},
        {"Ping_Color", "Ping Color"},
        {"Ping_Size", "Ping Size"},
        {"Play_Ping_Animation", "Play Ping Animation"},
        {"Free_Camera_Button", "Free Camera Button"},
        {"Dynamic_AI", "Dynamic AI"},
        {"Dynamic_AI_Range", "Dynamic AI Range"},
        {"Dynamic_AI_Rate", "Dynamic AI Rate"},
        {"Culling_System", "Culling System"},
        {"Culling_Range", "Culling Range"},
        {"Enforced_Spawn_Limits", "Enforced Spawn Limits"},
        {"Despawn_Furthest", "Despawn Furthest"},
        {"Max_Bots_Factory", "Max Bots Factory"},
        {"Max_Bots_Customs", "Max Bots Customs"},
        {"Max_Bots_Interchange", "Max Bots Interchange"},
        {"Max_Bots_Reserve", "Max Bots Reserve"},
        {"Max_Bots_Woods", "Max Bots Woods"},
        {"Max_Bots_Shoreline", "Max Bots Shoreline"},
        {"Max_Bots_Streets", "Max Bots Streets"},
        {"Max_Bots_Ground_Zero", "Max Bots Ground Zero"},
        {"Max_Bots_Labs", "Max Bots Labs"},
        {"Max_Bots_Lighthouse", "Max Bots Lighthouse"},
        {"Native_Sockets", "Native Sockets"},
        {"Force_IP", "Force IP"},
        {"Force_Bind_IP", "Force Bind IP"},
        {"Auto_Server_Refresh_Rate", "Auto Server Refresh Rate"},
        {"UDP_Port", "UDP Port"},
        {"Use_UPnP", "Use UPnP"},
        {"Head_Damage_Multiplier", "Head Damage Multiplier"},
        {"Armpit_Damage_Multiplier", "Armpit Damage Multiplier"},
    }},
    {"RU", new Dictionary<string, string>{
        {"Show_Feed", "Show Feed"},
        {"Auto_Extract", "Auto Extract"},
        {"Show_Extract_Message", "Show Extract Message"},
        {"Faster_Inventory_Scroll", "Faster Inventory Scroll"},
        {"Faster_Inventory_Scroll_Speed", "Faster Inventory Scroll Speed"},
        {"Show_Player_Name_Plates", "Show Player Name Plates"},
        {"Show_HP_Percentage_Instead_of_Bar", "Show HP Percentage Instead of Bar"},
        {"Show_Player_Faction_Icon", "Show Player Faction Icon"},
        {"Name_Plate_Scale", "Name Plate Scale"},
        {"Ping_System", "Ping System"},
        {"Ping_Button", "Ping Button"},
        {"Ping_Color", "Ping Color"},
        {"Ping_Size", "Ping Size"},
        {"Play_Ping_Animation", "Play Ping Animation"},
        {"Free_Camera_Button", "Free Camera Button"},
        {"Dynamic_AI", "Dynamic AI"},
        {"Dynamic_AI_Range", "Dynamic AI Range"},
        {"Dynamic_AI_Rate", "Dynamic AI Rate"},
        {"Culling_System", "Culling System"},
        {"Culling_Range", "Culling Range"},
        {"Enforced_Spawn_Limits", "Enforced Spawn Limits"},
        {"Despawn_Furthest", "Despawn Furthest"},
        {"Max_Bots_Factory", "Max Bots Factory"},
        {"Max_Bots_Customs", "Max Bots Customs"},
        {"Max_Bots_Interchange", "Max Bots Interchange"},
        {"Max_Bots_Reserve", "Max Bots Reserve"},
        {"Max_Bots_Woods", "Max Bots Woods"},
        {"Max_Bots_Shoreline", "Max Bots Shoreline"},
        {"Max_Bots_Streets", "Max Bots Streets"},
        {"Max_Bots_Ground_Zero", "Max Bots Ground Zero"},
        {"Max_Bots_Labs", "Max Bots Labs"},
        {"Max_Bots_Lighthouse", "Max Bots Lighthouse"},
        {"Native_Sockets", "Native Sockets"},
        {"Force_IP", "Force IP"},
        {"Force_Bind_IP", "Force Bind IP"},
        {"Auto_Server_Refresh_Rate", "Auto Server Refresh Rate"},
        {"UDP_Port", "UDP Port"},
        {"Use_UPnP", "Use UPnP"},
        {"Head_Damage_Multiplier", "Head Damage Multiplier"},
        {"Armpit_Damage_Multiplier", "Armpit Damage Multiplier"},
    }},
};


        public static string GetTranslatedString(string key)
        {
            // Default to English if the selected language is not found
            string lang = FikaPlugin.FikaLanguage.Value;
            if (!translations.ContainsKey(lang))
            {
                lang = "中文（简体）";
            }

            // Default to the original English text if the translation is not found
            if (translations[lang].ContainsKey(key))
            {
                return translations[lang][key];
            }
            else
            {
                return translations["中文（简体）"][key];
            }
        }
    }
}
