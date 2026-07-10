using UnityEngine;
using UnityEngine.VFX;
using System.Collections;

public class Effect : MonoBehaviour, IEffectExecuter
{
    [SerializeField] private ACTIVE_SKILL_EFFECT_TYPE type;     // 종류
    private VisualEffect effect;                                // 이펙트
    private Coroutine timerCoroutine;                           // 타이머 코루틴

    // 이펙트 초기화
    private void Awake() => effect = transform.GetComponent<VisualEffect>();

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
        // 이펙트가 없다면
        else if(effect == null)
        {
            // 활성화
            gameObject.SetActive(true);
            Debug.Log($"[Effect] 이펙트 없음 => 입력 - 이펙트 실행", this);
            return;
        }

        // 이펙트 실행
        effect.SendEvent("OnPlay");     // 파티클 시스템이라면 .Play();
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
        // 이펙트가 없다면
        else if (effect == null)
        {
            // 활성화
            gameObject.SetActive(true);
            // 타이머 실행
            timerCoroutine = StartCoroutine(TimerRoutine(time));
            Debug.Log($"[Effect] 이펙트 없음 => 입력 - 이펙트 실행({time:F02}초 이후 종료)", this);
            return;
        }

        // 이펙트 실행
        effect.SendEvent("OnPlay");
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

        // 이펙트가 없다면
        if (effect == null)
        {
            // 비활성화
            gameObject.SetActive(false);
            Debug.Log($"[Effect] 이펙트 없음 => 이펙트 자동 종료", this);
            yield break;
        }

        // 이펙트 종료
        effect.SendEvent("OnStop");     // 파티클 시스템이라면 .Stop()
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
        // 이펙트가 없다면
        else if (effect == null)
        {
            // 비활성화
            gameObject.SetActive(false);
            Debug.Log($"[Effect] 이펙트 없음 => 이펙트 종료", this);
            return;
        }

        // 이펙트 종료
        effect.SendEvent("OnStop");
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
        // 이펙트가 없다면
        else if (effect == null)
        {
            // 비활성화
            gameObject.SetActive(false);
            Debug.Log($"[Effect] 이펙트 없음 => 입력 - 이펙트 초기화", this);
            return;
        }

        // 이펙트 초기화
        effect.Reinit();        // 파티클 시스템이라면 .Clear()
    }
}