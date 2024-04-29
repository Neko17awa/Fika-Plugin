﻿#pragma warning disable CS0169
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonHandler : MonoBehaviour, IPointerExitHandler, IPointerEnterHandler
{
    private Image handleImage;
    private TextMeshProUGUI handleText;

    [SerializeField]
    Sprite buttonSprite;

    void Start()
    {
        handleImage = GetComponent<Image>();
        handleText = GetComponentInChildren<TextMeshProUGUI>();
    }

    private IEnumerator FadeButton()
    {
        while (handleImage.color.a > 0)
        {
            yield return new WaitForFixedUpdate();
            handleImage.color = new Color(255, 255, 255, handleImage.color.a - 0.15f);
            handleText.color = new Color(0, 0, 0, handleText.color.a - 0.15f);
        }
        while (handleText.color.a < 1)
        {
            yield return new WaitForFixedUpdate();
            handleText.color = new Color(255, 255, 255, handleText.color.a + 0.15f);
        }
    }

    private IEnumerator ShowButton()
    {
        while (handleText.color.a > 0)
        {
            yield return new WaitForFixedUpdate();
            handleText.color = new Color(255, 255, 255, handleText.color.a - 0.15f);
        }
        while (handleImage.color.a < 1)
        {
            yield return new WaitForFixedUpdate();
            handleImage.color = new Color(255, 255, 255, handleImage.color.a + 0.15f);
            handleText.color = new Color(0, 0, 0, handleText.color.a + 0.15f);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        StopAllCoroutines();
        StartCoroutine(ShowButton());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        StopAllCoroutines();
        StartCoroutine(FadeButton());
    }
}
