using UnityEngine;
using System.Collections;

//TimeScale 제어 매니저
public class TimeControlManager : MonoBehaviour, IInitializable
{
    public int Priority => (int)InitOrder.System;

    private Coroutine timeCoroutine; //현재 실행중인 시간정지 메모리블록
    private float originalFixedDelta;   //물리 엔진 기본 1틱 시간
    private float timeEventLockDuration = 2f; //타임 스케일 조절 이벤트 잠금 시간
    private float curTime = 0f; //타임스케일 다중 접근 방지
    private bool publishTimeEvent = false; //타임 이벤트 발행되었는지 확인

    public void Init()
    {
        //물리엔진 기본틱(0.02f)을 메모리 캐싱
        originalFixedDelta = Time.fixedDeltaTime;
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

        //안전장치: 다음 씬이 멈춘채로 시작 방지
        if(timeCoroutine != null)
        {
            StopCoroutine(timeCoroutine);
            RestoreTime();
        }
    }

    private void Update()
    {
        
    }

    //히트스탑 처리
    public void TriggerHitStop(HitStopEvent data)
    {
        //Debug.Log(data.frames);
        //안전장치: 연속 패링으로 인해 또 이벤트가 들어올 경우 방지
        if (timeCoroutine != null)
        {
            //기존 진행 프레임 카운팅 파괴
            StopCoroutine(timeCoroutine);
            //시간 배율 초기화
            RestoreTime();
        }
        //새로운 코루틴 스레드 시작
        timeCoroutine = StartCoroutine(HitStopRoutine(data.frames));
    }

    private IEnumerator HitStopRoutine(int frames) //몇 프레임 멈출것인지
    {
        //시간 정지: 0.01f로 Jitter 버그 방지
        Time.timeScale = 0.01f;
        //물리 동기화: 충돌 연산 주기도 동일한 배율로 늦춰 터널링 현상 방지
        Time.fixedDeltaTime = originalFixedDelta * Time.timeScale;
        //프레임 카운팅
        //엔진 시간은 멈췄지만 메인 스레드의 렌더링 루프는 계속 돌기 때문에 프레임 카운팅 가능
        for (int i = 0; i < frames; i++) yield return null;
        //시간 복구
        RestoreTime();
    }

    //불릿타임 처리
    public void TriggerSlowMo(SlowMoEvent data)
    {
        if (Time.timeScale <= 0.1f) return;
        if (timeCoroutine != null) StopCoroutine(timeCoroutine);
        timeCoroutine = StartCoroutine(SlowMoRoutine(data.targetScale, data.durationRealtime));
    }
    private IEnumerator SlowMoRoutine(float targetScale, float duration)
    {
        Time.timeScale = targetScale;
        Time.fixedDeltaTime = originalFixedDelta * Time.timeScale;
        //물리 시간 측정되도록 UnScaledTime사용
        yield return new WaitForSecondsRealtime(duration);
        RestoreTime();
    }

    //timeScale 초기화
    private void RestoreTime()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = originalFixedDelta;
        timeCoroutine = null;
    }
}
