using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Pool;

public class Bullet : MonoBehaviour, IPoolable
{
    [Header("최대 사거리")]
    [SerializeField] private float maxRange = 30f;          // 최대 사거리
    [Header("최대 유지 시간")]
    [SerializeField] private float maxLifeTime = 5f;        // 최대 유지 시간

    private LayerMask ownerLayer;                           // 소유자 레이어
    private float damage;                                   // 데미지
    private int penetrationCount;                           // 관통 횟수
    public float bulletSpeed;                              // 총알 속도

    private IObjectPool<GameObject> returnRef;              // 반납 오브젝트 풀 주소

    private Coroutine timerCoroutine;                       // 타이머 코루틴
    private WaitForSeconds returnTime;                      // 반납 시간

    private bool startFire = false;                         // 사격 시작 여부
    private bool isReturnedToPool;                          // 풀 반환 완료 여부

    private void Update()
    {
        // 사격이 시작되지 않았다면
        if (!startFire)
            return;

        // 전방으로 총알 발사
        transform.position += bulletSpeed * Time.deltaTime * transform.forward;
    }

    private void OnTriggerEnter(Collider other)
    {
        // 사격이 시작되지 않았거나, 풀에 반환되었다면
        if (!startFire || isReturnedToPool)
            return;

        // 부딪힌 대상의 최상위로 이동 후 하위 오브젝트 중, 데미지를 입을 수 있는 물체가 있는지 받아오기
        var target = other.transform.root.GetComponentInChildren<IDamageable>();

        // 데미지를 입을 수 없는 물체라면
        if (target == null)
        {
            // 총알 반납
            ReturnToPool();
            return;
        }

        // 부딪힌 물체의 레이어와 설정한 소유자 레이어와 같다면
        if (EqualsToOwnerLayer(other))
            return;

        // 부딪힌 대상에게 데미지 전달하기
        target.TakeDamage(damage);

        // 무한 관통이 아니라면
        if (penetrationCount != -1)
        {
            // 관통 횟수 감소
            penetrationCount = Math.Max(-1, penetrationCount - 1);

            // 관통 횟수가 남아있지 않다면
            if (penetrationCount <= -1)
                // 총알 반납
                ReturnToPool();
        }
    }

    private void OnDisable()
    {
        // 타이머 코루틴이 비어있지 않다면
        if (timerCoroutine != null)
        {
            // 타이머 중지
            StopCoroutine(timerCoroutine);
            // 타이머 코루틴 초기화
            timerCoroutine = null;
        }
    }

    /// <summary>
    /// 총알 반납 함수
    /// </summary>
    private void ReturnToPool()
    {
        // 풀에 반환되었다면
        if (isReturnedToPool)
            return;

        // 풀 반환 시작
        isReturnedToPool = true;
        // 사격 종료
        startFire = false;

        // 타이머 코루틴이 비어있지 않다면
        if (timerCoroutine != null)
        {
            // 타이머 코루틴 중지
            StopCoroutine(timerCoroutine);
            // 타이머 코루틴 초기화
            timerCoroutine = null;
        }

        // 하위 오브젝트가 있다면
        if (transform.childCount > 0)
        {
            // 하위 오브젝트들의 이펙트 실행기 인터페이스들 받아오기
            var executers = transform.GetComponentsInChildren<IEffectExecuter>(true);

            // 이펙트 실행기들의 수만큼
            foreach (var executer in executers)
                // 이펙트 종료 및 반납
                executer.StopEffect();
        }

        // 총알 반납하기
        returnRef.Release(gameObject);
    }

    /// <summary>
    /// 레이어 비교 함수(레이어가 같으면 true 반환)
    /// </summary>
    /// <param name="hit">부딪힌 오브젝트</param>
    private bool EqualsToOwnerLayer(Collider hit)
        => ((1 << hit.gameObject.layer) & ownerLayer.value) != 0;

    /// <summary>
    /// 오브젝트 풀 주소 설정 함수
    /// </summary>
    /// <param name="poolRef">반환 주소</param>
    public void SetPoolRef(IObjectPool<GameObject> poolRef) => returnRef = poolRef;

    /// <summary>
    /// 사격 시작 함수
    /// </summary>
    /// <param name="spawnPoint">생성 장소</param>
    /// <param name="ownerLayer">소유자 레이어</param>
    /// <param name="damage">데미지</param>
    /// <param name="penetrationCount">관통 횟수(생략 가능, 기본값 : 0)</param>
    /// <param name="speed">총알 속도(생략 가능, 기본값 : 10f)</param>
    public void StartFire(Transform spawnPoint, LayerMask ownerLayer, float damage,
                                            int penetrationCount = 0, float speed = 10f)
    {
        // 타이머 코루틴이 비어있지 않다면
        if (timerCoroutine != null)
        {
            // 타이머 중지
            StopCoroutine(timerCoroutine);
            // 타이머 코루틴 초기화
            timerCoroutine = null;
        }

        // 총알의 위치, 각도 설정
        transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
        // 총알 정보 설정
        SetInfo(ownerLayer, damage, penetrationCount, speed);
        // 사격 시작
        startFire = true;
        // 풀 반환 완료 여부 초기화
        isReturnedToPool = false;
        // 최대 유지 시간 설정
        //maxLifeTime = maxRange / speed;
        // 반납 시간 초기화
        returnTime = new WaitForSeconds(maxLifeTime);
        // 타이머 시작
        timerCoroutine = StartCoroutine(ReturnRoutine());
    }

    /// <summary>
    /// 사격 시작 함수(위치+각도 설정이 필요가 없거나, 이미 위치+각도 설정을 했음)
    /// </summary>
    /// <param name="ownerLayer">소유자 레이어</param>
    /// <param name="damage">데미지</param>
    /// <param name="penetrationCount">관통 횟수(생략 가능, 기본값 : 0)</param>
    /// <param name="speed">총알 속도(생략 가능, 기본값 : 10f)</param>
    public void StartFire(LayerMask ownerLayer, float damage, int penetrationCount = 0, float speed = 10f)
                    => StartFire(transform, ownerLayer, damage, penetrationCount, speed);

    /// <summary>
    /// 총알 정보 설정 함수
    /// </summary>
    /// <param name="layer">소유자 레이어</param>
    /// <param name="amount">데미지</param>
    /// <param name="count">관통 횟수</param>
    /// <param name="speed">총알 속도</param>
    private void SetInfo(LayerMask layer, float amount, int count, float speed)
    {
        // 소유자 레이어 설정
        ownerLayer = layer;
        // 데미지 설정
        damage = amount;
        // 관통 횟수 설정
        penetrationCount = count;
        // 총알 속도 설정
        //bulletSpeed = speed;
    }

    /// <summary>
    /// 타이머 코루틴 함수
    /// </summary>
    private IEnumerator ReturnRoutine()
    {
        // 반납 시간 기다리기
        yield return returnTime;
        // 타이머 코루틴 초기화
        timerCoroutine = null;
        // 총알 반납
        ReturnToPool();
    }
}