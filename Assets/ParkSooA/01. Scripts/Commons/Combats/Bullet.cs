using UnityEngine;
using UnityEngine.Pool;
using System;
using System.Collections;

public class Bullet : MonoBehaviour, IPoolable
{
    [Header("총알 스탯")]
    [Header("소유자 레이어")]
    [Tooltip("총알을 발사한 주체의 레이어를 저장하여, 부딪힌 물체를 구분할 예정임")]
    [SerializeField] private LayerMask ownerLayer;          // 소유자 레이어
    [Header("데미지")]
    [SerializeField] private float damage = 0f;             // 데미지
    [Header("관통 횟수")]
    [Tooltip("-1로 두면 무한 관통, 0으로 두면 관통 0회, 1로 두면 관통 1회 스킬이 됨")]
    [SerializeField] private int penetrationCount = 0;      // 관통 횟수
    [Header("속도")]
    [Tooltip("날아가는 (임시)속도, 얼마든지 조정하셔도 됨")]
    [SerializeField] private float speed = 10f;             // 속도
    [Header("지속 시간")]
    [Tooltip("날아가는 총알이 유지되는 시간, 얼마든지 조장하셔도 됨")]
    [SerializeField] private float duration = 5f;           // 지속 시간

    private IObjectPool<GameObject> returnRef;              // 반납할 오브젝트 풀 주소

    private Coroutine timerCoroutine;                       // 타이머 코루틴
    private WaitForSeconds returnTime;                      // 반납 시간

    private bool startFire = false;                         // 사격 시작 여부

    private void Update()
    {
        // 사격이 시작되지 않았다면
        if (!startFire)
            return;

        // 전방으로 총알 발사
        transform.position += speed * Time.deltaTime * transform.forward;
    }

    private void OnTriggerEnter(Collider other)
    {
        // 닿은 물체의 레이어가 소유자 레이어와 같다면
        if (((1 << other.gameObject.layer) & ownerLayer.value) != 0)
            return;
        // 데미지를 입을 수 없는 물체라면
        else if (!other.TryGetComponent<IDamageable>(out var target))
            // 총알 반납
            returnRef.Release(gameObject);
        else
        {
            // 데미지 전달
            target.TakeDamage(damage);

            // 무한 관통이 아니라면
            if (penetrationCount != -1)
            {
                // 관통 횟수 감소
                penetrationCount = Math.Max(-1, penetrationCount - 1);

                // 관통 횟수가 남아있지 않다면
                if (penetrationCount <= -1)
                {
                    if(transform.childCount > 0)
                    {
                        var executers = transform.GetComponentsInChildren<IEffectExecuter>(true);

                        foreach (var executer in executers)
                            executer.StopEffect();
                    }

                    // 총알 반납
                    returnRef.Release(gameObject);
                }
            }
        }
    }

    private void OnDisable()
    {
        // 타이머 코루틴이 비어있지 않다면
        if(timerCoroutine != null)
        {
            // 타이머 중지
            StopCoroutine(timerCoroutine);
            // 타이머 코루틴 초기화
            timerCoroutine = null;
        }
    }

    /// <summary>
    /// 오브젝트 풀 주소 설정 함수(반환 주소 설정)
    /// </summary>
    public void SetPoolRef(IObjectPool<GameObject> poolRef) => returnRef = poolRef;

    /// <summary>
    /// 사격 시작 함수
    /// </summary>
    public void StartFire(Transform origin, LayerMask ownerLayer, float damage,
                                                            int penetrationCount = 0)
    {
        // 타이머 코루틴이 비어있지 않다면
        if (timerCoroutine != null)
            // 타이머 중지
            StopCoroutine(timerCoroutine);

        // 위치, 각도 설정
        transform.SetPositionAndRotation(origin.position, origin.rotation);
        // 총알 정보 설정
        SetInfo(ownerLayer, damage, penetrationCount);
        // 사격 시작
        startFire = true;
        // 반납 시간 초기화
        returnTime = new WaitForSeconds(duration);
        // 타이머 시작
        timerCoroutine = StartCoroutine(ReturnRoutine());
    }

    /// <summary>
    /// 총알 정보 설정 함수
    /// </summary>
    private void SetInfo(LayerMask layer, float amount, int count)
    {
        // 소유자 레이어 설정
        ownerLayer = layer;
        // 데미지 설정
        damage = amount;
        // 관통 횟수 설정
        penetrationCount = count;
    }

    /// <summary>
    /// 타이머 코루틴 함수
    /// </summary>
    private IEnumerator ReturnRoutine()
    {
        // 반납 시간 기다리기
        yield return returnTime;
        // 사격 종료
        startFire = false;
        // 타이머 코루틴 초기화
        timerCoroutine = null;

        if (transform.childCount > 0)
        {
            var executers = transform.GetComponentsInChildren<IEffectExecuter>(true);

            foreach (var executer in executers)
                executer.StopEffect();
        }

        // 총알 반납
        returnRef.Release(gameObject);
    }
}