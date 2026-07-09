using UnityEngine;
using System.Collections;

// Time.timeScale을 직접 만지는 유일한 관리자.
// 히트스탑/불릿타임 요청을 우선순위와 배타 그룹으로 정리해 연출끼리 겹치는 문제를 막는다.
public class TimeControlManager : MonoBehaviour, IInitializable
{
    private enum ActiveTimeEffectType { None, HitStop, SlowMo }

    public int Priority => (int)InitOrder.System;

    [Header("Conflict Policy")]
    [Tooltip("패링 히트스탑 직후 사전신호 불릿타임을 무시할 실제 시간. 0.1~0.15초 정도가 자연스럽다.")]
    [SerializeField, Min(0f)] private float telegraphSlowMoSuppressAfterHitStop = 0.12f;
    [Tooltip("히트스탑 중 완전 0으로 만들면 일부 물리/애니메이션이 튈 수 있어 아주 작은 값으로 멈춘다.")]
    [SerializeField, Range(0.001f, 0.1f)] private float hitStopScale = 0.01f;

    private Coroutine timeCoroutine; // 현재 실행 중인 시간 연출 코루틴
    private float originalFixedDelta; // 물리 엔진의 기본 fixedDeltaTime
    private ActiveTimeEffectType activeEffectType = ActiveTimeEffectType.None;
    private TimeEffectPriority activePriority = TimeEffectPriority.Low;
    private string activeExclusiveGroup = string.Empty;
    private float suppressTelegraphSlowMoUntilRealtime = 0f;

    public void Init()
    {
        CacheOriginalFixedDelta();
    }

    private void Awake()
    {
        // Init 순서가 바뀌거나 테스트 씬에서 매니저만 단독 배치되어도 복구 기준 시간이 비지 않게 한다.
        CacheOriginalFixedDelta();
    }

    private void OnEnable()
    {
        EventBus<HitStopEvent>.action += TriggerHitStop;
        EventBus<SlowMoEvent>.action += TriggerSlowMo;
    }

    private void OnDisable()
    {
        EventBus<HitStopEvent>.action -= TriggerHitStop;
        EventBus<SlowMoEvent>.action -= TriggerSlowMo;

        if (timeCoroutine != null)
            StopCoroutine(timeCoroutine);

        RestoreTime();
    }

    // 히트스탑은 패링/피격 성공의 즉각 보상이므로 같은 CombatFeel 그룹의 불릿타임보다 우선한다.
    public void TriggerHitStop(HitStopEvent data)
    {
        if (!CanStartEffect(data.priority, data.exclusiveGroup))
            return;

        StopActiveEffect();

        // 히트스탑 직후 들어오는 사전신호 슬로모는 손맛을 흐릴 수 있어 짧게 무시한다.
        if (data.source == TimeEffectSource.Parry || data.source == TimeEffectSource.Impact)
            suppressTelegraphSlowMoUntilRealtime = Time.realtimeSinceStartup + telegraphSlowMoSuppressAfterHitStop;

        BeginEffect(ActiveTimeEffectType.HitStop, data.priority, data.exclusiveGroup);
        timeCoroutine = StartCoroutine(HitStopRoutine(Mathf.Max(1, data.frames)));
    }

    private IEnumerator HitStopRoutine(int frames)
    {
        ApplyTimeScale(hitStopScale);

        // 히트스탑은 "실제 렌더 프레임 수"만큼 유지한다.
        // Time.timeScale이 거의 0이어도 yield return null은 다음 프레임으로 넘어가므로 입력 피드백이 일정하다.
        for (int i = 0; i < frames; i++)
            yield return null;

        RestoreTime();
    }

    // 불릿타임은 보조 연출이다. 히트스탑이 진행 중이거나 방금 끝났다면 실행하지 않는다.
    public void TriggerSlowMo(SlowMoEvent data)
    {
        if (data.durationRealtime <= 0f)
            return;

        if (data.source == TimeEffectSource.Telegraph &&
            Time.realtimeSinceStartup < suppressTelegraphSlowMoUntilRealtime)
            return;

        if (!CanStartEffect(data.priority, data.exclusiveGroup))
            return;

        StopActiveEffect();

        BeginEffect(ActiveTimeEffectType.SlowMo, data.priority, data.exclusiveGroup);
        timeCoroutine = StartCoroutine(SlowMoRoutine(Mathf.Clamp(data.targetScale, 0.01f, 1f), data.durationRealtime));
    }

    private IEnumerator SlowMoRoutine(float targetScale, float duration)
    {
        ApplyTimeScale(targetScale);

        // 불릿타임 지속시간은 게임 속도에 영향을 받으면 안 되므로 실시간 기준으로 기다린다.
        yield return new WaitForSecondsRealtime(duration);

        RestoreTime();
    }

    private bool CanStartEffect(TimeEffectPriority requestedPriority, string requestedGroup)
    {
        if (timeCoroutine == null || activeEffectType == ActiveTimeEffectType.None)
            return true;

        // 전역 timeScale은 하나뿐이므로 그룹이 달라도 낮은 우선순위 연출이 높은 연출을 덮지 못하게 한다.
        if ((int)requestedPriority < (int)activePriority)
            return false;

        // 같은 그룹의 같은 우선순위는 새 요청으로 교체한다. 연속 타격/연속 패링의 즉시성을 살리기 위함이다.
        if (requestedGroup == activeExclusiveGroup)
            return true;

        return (int)requestedPriority >= (int)activePriority;
    }

    private void BeginEffect(ActiveTimeEffectType type, TimeEffectPriority priority, string exclusiveGroup)
    {
        activeEffectType = type;
        activePriority = priority;
        activeExclusiveGroup = exclusiveGroup ?? string.Empty;
    }

    private void StopActiveEffect()
    {
        if (timeCoroutine == null)
            return;

        StopCoroutine(timeCoroutine);
        RestoreTime();
    }

    private void ApplyTimeScale(float timeScale)
    {
        CacheOriginalFixedDelta();
        Time.timeScale = timeScale;
        Time.fixedDeltaTime = originalFixedDelta * Time.timeScale;
    }

    private void RestoreTime()
    {
        CacheOriginalFixedDelta();
        Time.timeScale = 1f;
        Time.fixedDeltaTime = originalFixedDelta;
        timeCoroutine = null;
        activeEffectType = ActiveTimeEffectType.None;
        activePriority = TimeEffectPriority.Low;
        activeExclusiveGroup = string.Empty;
    }

    private void CacheOriginalFixedDelta()
    {
        if (originalFixedDelta <= 0f)
            originalFixedDelta = Time.fixedDeltaTime;
    }
}
