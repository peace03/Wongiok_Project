using System;
using System.Collections;
using UnityEngine;

public class ScreenFader : MonoBehaviour, IInitializable
{
    [SerializeField] private CanvasGroup fadeCanvasGroup;

    private Coroutine fadeCoroutine;

    // 초기화 순서
    public int Priority => (int)InitOrder.UI + 20;

    public void Init()
    {
        SetAlpha(0f);
        SetInputBlock(false);
        SubscribeEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    // EventBus 구독
    private void SubscribeEvents()
    {
        EventBus<UIFadeEvent>.action += HandleFade;
    }

    // EventBus 구독 해제
    private void UnsubscribeEvents()
    {
        EventBus<UIFadeEvent>.action -= HandleFade;
    }

    // UIOpenEvent 수신 시 페이드 연출 시작
    private void HandleFade(UIFadeEvent eventData)
    {
        StartFade(
            eventData.FromAlpha,
            eventData.ToAlpha,
            eventData.Duration,
            eventData.OnComplete);
    }

    public void StartFade(
        float fromAlpha,
        float toAlpha,
        float duration,
        Action onComplete = null)
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(FadeRoutine(fromAlpha, toAlpha, duration, onComplete));
    }

    private IEnumerator FadeRoutine(
        float fromAlpha,
        float toAlpha,
        float duration,
        Action onComplete)
    {
        SetInputBlock(true);
        SetAlpha(fromAlpha);

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            // Time.unscaledDeltaTime을 사용하여 게임이 일시정지 상태에서도 페이드가 정상적으로 진행되도록 방지
            // Time.unscaledDeltaTime: Time.deltaTime과 달리 Time.timeScale의 영향을 받지 않는 델타 타임
            elapsedTime += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(elapsedTime / duration);
            float alpha = Mathf.Lerp(fromAlpha, toAlpha, progress);

            SetAlpha(alpha);

            yield return null;
        }

        SetAlpha(toAlpha);
        SetInputBlock(toAlpha > 0f);

        fadeCoroutine = null;
        onComplete?.Invoke();
    }

    private void SetInputBlock(bool shouldBlock)
    {
        if (fadeCanvasGroup == null)
            return;

        // 페이드 연출 중에 플레이어의 입력을 차단
        fadeCanvasGroup.blocksRaycasts = shouldBlock;
        fadeCanvasGroup.interactable = shouldBlock;
    }

    private void SetAlpha(float alpha)
    {
        if (fadeCanvasGroup == null)
            return;

        fadeCanvasGroup.alpha = alpha;
    }
}
