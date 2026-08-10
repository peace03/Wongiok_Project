using System;
using System.Collections;
using UnityEngine;

public class ScreenFader : MonoBehaviour, IInitializable
{
    [SerializeField] private CanvasGroup fadeCanvasGroup;

    private static bool hasPendingSceneFadeIn;
    private static float pendingSceneFadeInDuration;

    private Coroutine fadeCoroutine;

    // 초기화 순서
    public int Priority => (int)InitOrder.UI + 20;

    // 2026.08.10_UI 정리: 이 메서드의 UI 처리 역할을 수행한다.
    public void Init()
    {
        bool shouldFadeInAfterSceneLoad = TryConsumeSceneFadeIn(
            out float sceneFadeInDuration);

        SetAlpha(shouldFadeInAfterSceneLoad ? 1f : 0f);
        SetInputBlock(shouldFadeInAfterSceneLoad);
        SubscribeEvents();

        if (shouldFadeInAfterSceneLoad)
            StartCoroutine(FadeInAfterFirstSceneFrame(sceneFadeInDuration));
    }

    // 2026.08.10_씬 활성화 중에도 검은 화면을 유지하도록 다음 Fader에 전환 정보를 전달한다.
    public static void HoldBlackForNextScene(float fadeInDuration)
    {
        hasPendingSceneFadeIn = true;
        pendingSceneFadeInDuration = Mathf.Max(0f, fadeInDuration);
    }

    // 2026.08.10_UI 정리: 파괴 시 등록한 이벤트와 임시 UI 상태를 정리한다.
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

    // 2026.08.10_UI 정리: 이 메서드의 UI 처리 역할을 수행한다.
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

    // 2026.08.10_UI 정리: 이 메서드의 UI 처리 역할을 수행한다.
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

    // 2026.08.10_새 씬의 첫 프레임이 그려진 뒤에만 검은 전환 화면을 걷는다.
    private IEnumerator FadeInAfterFirstSceneFrame(float duration)
    {
        yield return new WaitForEndOfFrame();
        StartFade(1f, 0f, duration);
    }

    // 2026.08.10_다음 씬 전용 검은 화면 유지 요청을 한 번만 소비한다.
    private static bool TryConsumeSceneFadeIn(out float fadeInDuration)
    {
        fadeInDuration = pendingSceneFadeInDuration;

        if (!hasPendingSceneFadeIn)
            return false;

        hasPendingSceneFadeIn = false;
        pendingSceneFadeInDuration = 0f;
        return true;
    }

    // 2026.08.10_UI 정리: 입력 Block 표시 값을 반영한다.
    private void SetInputBlock(bool shouldBlock)
    {
        if (fadeCanvasGroup == null)
            return;

        // 페이드 연출 중에 플레이어의 입력을 차단
        fadeCanvasGroup.blocksRaycasts = shouldBlock;
        fadeCanvasGroup.interactable = shouldBlock;
    }

    // 2026.08.10_UI 정리: Alpha 표시 값을 반영한다.
    private void SetAlpha(float alpha)
    {
        if (fadeCanvasGroup == null)
            return;

        fadeCanvasGroup.alpha = alpha;
    }
}
