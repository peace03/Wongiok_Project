using UnityEngine;
using UnityEngine.Pool;
using System.Collections;

public class Effect : MonoBehaviour, IPoolable, IEffectExecuter
{
    private IObjectPool<GameObject> returnRef;      // 반납할 오브젝트 풀 주소

    private Transform container;                    // 컨테이너
    private ParticleSystem particle;                // 파티클
    private Coroutine timerCoroutine;               // 타이머 코루틴

    private float maxEffectTime = 0f;               // 최대 이펙트 시간

    public Transform Container => container;
    public float MaxEffectTime => maxEffectTime;

    private void Awake()
    {
        // 파티클 받아오기
        particle = transform.GetComponent<ParticleSystem>();
        // 컨테이너 받아오기
        container = transform.parent;

        // 파티클이 있다면
        if (particle != null)
            // 최대 이펙트 시간 받아오기
            maxEffectTime = particle.main.startLifetime.constantMax;
    }

    private void OnDisable()
    {
        // 이펙트 초기화(남은 잔상 지우기)
        ResetEffect();
        // 이펙트 반납하기
        returnRef?.Release(gameObject);
    }

    /// <summary>
    /// 반납할 오브젝트 풀 주소 설정 함수
    /// </summary>
    /// <param name="poolRef">반납할 오브젝트 풀 주소</param>
    public void SetPoolRef(IObjectPool<GameObject> poolRef) => returnRef = poolRef;

    /// <summary>
    /// 이펙트 실행 함수
    /// </summary>
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

    /// <summary>
    /// 이펙트 실행 함수
    /// </summary>
    /// <param name="time">이펙트 종료 시간</param>
    public void ExecuteEffect(float time)
    {
        // 이펙트 실행
        ExecuteEffect();
        // 이펙트 종료 시간으로 타이머 실행
        timerCoroutine = StartCoroutine(TimerRoutine(time));
    }

    /// <summary>
    /// 타이머 코루틴 함수
    /// </summary>
    /// <param name="time">타이머 시간</param>
    private IEnumerator TimerRoutine(float time)
    {
        // 파티클이 있다면
        if (particle != null)
        {
            // 타이머 시간이 최대 이펙트 시간보다 크다면
            if(time - maxEffectTime > 0f)
            {
                // 타이머 시간 중 최대 이펙트 시간을 제외한 나머지 시간 대기하기
                yield return new WaitForSeconds(time - maxEffectTime);
                // 파티클 종료
                particle.Stop();
                // 최대 이펙트 시간만큼 대기하기
                yield return new WaitForSeconds(maxEffectTime);
            }
            // 타이머 시간이 최대 이펙트 시간보다 작거나 같다면
            else
            {
                // 파티클이 재생될 수 있게 잠시 대기하기
                yield return new WaitForSeconds(0.1f);
                // 파티클 종료
                particle.Stop();
                // 나머지 타이머 시간만큼 대기하기
                yield return new WaitForSeconds(time - 0.1f);
            }
        }
        // 파티클이 없다면
        else
            // 타이머 시간만큼 대기하기
            yield return new WaitForSeconds(time);

        // 타이머 코루틴 초기화
        timerCoroutine = null;

        // 따라다니고 있는 대상이 있다면
        if (transform.parent != container)
            // 컨테이너로 돌려보내기
            transform.SetParent(container, true);

        // 오브젝트 비활성화
        gameObject.SetActive(false);
    }

    public void PauseEffect()
    {
        if (transform.childCount > 0)
        {
            var particles = transform.GetComponentsInChildren<ParticleSystem>();

            if (particles == null || particles.Length == 0)
                return;

            for (int i = 0; i < particles.Length; i++)
            {
                particles[i].Pause();

                if (particles[i].main.simulationSpace == ParticleSystemSimulationSpace.Local)
                    continue;

                var module = particles[i].main;
                module.simulationSpace = ParticleSystemSimulationSpace.Local;
            }
        }
        else if (particle != null)
        {
            particle.Pause();

            if(particle.main.simulationSpace != ParticleSystemSimulationSpace.Local)
            {
                var module = particle.main;
                module.simulationSpace = ParticleSystemSimulationSpace.Local;
            }
        }
    }

    /// <summary>
    /// 이펙트 종료 함수
    /// </summary>
    /// <param name="immediately">즉시 종료 여부(기본값 : 즉시 종료 안함)</param>
    public void StopEffect(bool immediately = false)
    {
        // 타이머 코루틴이 비어있지 않다면
        if (timerCoroutine != null)
        {
            // 타이머 코루틴 중지
            StopCoroutine(timerCoroutine);
            // 타이머 코루틴 초기화
            timerCoroutine = null;
        }

        // 따라다니고 있는 대상이 있다면
        if (transform.parent != container)
            // 컨테이너로 돌려보내기
            transform.SetParent(container, true);

        // 파티클이 있다면
        if (particle != null)
        {
            // 즉시 종료가 아니라면
            if (!immediately)
                // 최대 이펙트 시간으로 타이머 실행
                timerCoroutine = StartCoroutine(TimerRoutine(maxEffectTime));
            // 즉시 종료라면
            else
                // 오브젝트 비활성화
                gameObject.SetActive(false);
        }
        // 파티클이 없다면
        else
            // 오브젝트 비활성화
            gameObject.SetActive(false);
    }

    /// <summary>
    /// 이펙트 초기화 함수
    /// </summary>
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
        {
            // 파티클 종료
            particle.Stop();
            // 파티클 초기화
            particle.Clear();
        }
    }
}