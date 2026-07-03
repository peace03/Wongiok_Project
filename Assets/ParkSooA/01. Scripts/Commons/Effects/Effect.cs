using UnityEngine;
using UnityEngine.Pool;
using System.Collections;

public class Effect : MonoBehaviour, IPoolable, IEffectExecuter
{
    private IObjectPool<GameObject> returnRef;      // 반납할 오브젝트 풀 주소

    private Transform container;                    // 컨테이너
    private ParticleSystem particle;                // 파티클
    private Coroutine timerCoroutine;               // 타이머 코루틴

    private void Awake()
    {
        // 파티클
        particle = transform.GetComponent<ParticleSystem>();
        container = transform.parent;
    }

    private void OnDisable()
    {
        // 이펙트 초기화(남은 잔상 지우기)
        ResetEffect();

        // 타이머 코루틴이 비어있지 않다면
        if (timerCoroutine != null)
        {
            // 타이머 코루틴 중지
            StopCoroutine(timerCoroutine);
            // 타이머 코루틴 초기화
            timerCoroutine = null;
        }

        // 따라다니고 있는 대상이 있다면
        if(transform.parent != container)
            // 컨테이너로 돌려보내기
            transform.SetParent(container, true);

        // 이펙트 반납하기
        returnRef?.Release(gameObject);
    }

    // 반납할 오브젝트 풀 주소 설정 함수
    public void SetPoolRef(IObjectPool<GameObject> poolRef) => returnRef = poolRef;

    // 이펙트 실행 함수
    public void ExecuteEffect()
    {
        // 타이머 코루틴이 비어있지 않다면
        if(timerCoroutine != null)
        {
            // 타이머 코루틴 중지
            StopCoroutine(timerCoroutine);
            // 타이머 코루틴 초기화
            timerCoroutine = null;
        }

        // 파티클이 있다면
        if(particle != null)
            // 파티클 실행
            particle.Play();
    }

    // 이펙트 실행 함수(time 초 이후 종료)
    public void ExecuteEffect(float time)
    {
        // 타이머 코루틴이 비어있지 않다면
        if (timerCoroutine != null)
        {
            // 타이머 코루틴 중지
            StopCoroutine(timerCoroutine);
            // 타이머 코루틴 초기화
            timerCoroutine = null;
        }

        // 파티클이 있다면
        if (particle != null)
            // 파티클 실행
            particle.Play();

        // 타이머 실행
        timerCoroutine = StartCoroutine(TimerRoutine(time));
    }

    /// <summary>
    /// 타이머 코루틴 함수
    /// </summary>
    private IEnumerator TimerRoutine(float time)
    {
        // 종료 시간 대기하기
        yield return new WaitForSeconds(time);

        // 파티클 있다면
        if (particle != null)
            // 파티클 종료
            particle.Stop();

        // 타이머 코루틴 초기화
        timerCoroutine = null;
    }

    // 이펙트 종료 함수
    public void StopEffect()
    {
        // 타이머 코루틴이 비어있지 않다면
        if (timerCoroutine != null)
        {
            // 타이머 코루틴 중지
            StopCoroutine(timerCoroutine);
            // 타이머 코루틴 초기화
            timerCoroutine = null;
        }

        // 파티클이 있다면
        if (particle != null)
            // 파티클 종료
            particle.Stop();
    }

    // 이펙트 초기화 함수
    public void ResetEffect()
    {
        // 타이머 코루틴이 비어있지 않다면
        if (timerCoroutine != null)
        {
            // 타이머 코루틴 중지
            StopCoroutine(timerCoroutine);
            // 타이머 코루틴 초기화
            timerCoroutine = null;
        }

        // 파티클이 있다면
        if (particle != null)
            // 파티클 초기화
            particle.Clear();
    }
}