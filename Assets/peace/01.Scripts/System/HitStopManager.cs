using UnityEngine;
using System.Collections;

public class HitStopManager : MonoBehaviour, IInitializable
{
    public int Priority => (int)InitOrder.System;

    private Coroutine hitStopCoroutine; //현재 실행중인 시간정지 메모리블록
    private float originalFixedDelta;   //물리 엔진 기본 1틱 시간

    public void Init()
    {
        //물리엔진 기본틱(0.02f)을 메모리 캐싱
        originalFixedDelta = Time.fixedDeltaTime;
    }

    private void OnEnable()
    {
        EventBus<HitStopEvent>.action += TriggerHitStop;
    }
    private void OnDisable()
    {
        EventBus<HitStopEvent>.action -= TriggerHitStop;

        //안전장치: 다음 씬이 멈춘채로 시작 방지
        if(hitStopCoroutine != null)
        {
            StopCoroutine(hitStopCoroutine);
            RestoreTime();
        }
    }

    public void TriggerHitStop(HitStopEvent data)
    {
        Debug.Log(data.frames);
        //안전장치: 연속 패링으로 인해 또 이벤트가 들어올 경우 방지
        if (hitStopCoroutine != null)
        {
            //기존 진행 프레임 카운팅 파괴
            StopCoroutine(hitStopCoroutine);
            //시간 배율 초기화
            RestoreTime();
        }
        //새로운 코루틴 스레드 시작
        hitStopCoroutine = StartCoroutine(HitStopRoutine(data.frames));
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

    //
    private void RestoreTime()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = originalFixedDelta;
        hitStopCoroutine = null;
    }
}
