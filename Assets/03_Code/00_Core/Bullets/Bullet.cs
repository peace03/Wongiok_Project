using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class Bullet : MonoBehaviour, IPoolable
{
    #region 변수
    [Header("타격/피격 이펙트 프리팹들")]
    [SerializeField] private List<GameObject> hitEffects;           // 타격/피격 이펙트 프리팹들
    [Header("최대 사거리")]
    [SerializeField] private float maxRange = 30f;                  // 최대 사거리
    [Header("최대 유지 시간")]
    [SerializeField] private float maxLifeTime = 5f;                // 최대 유지 시간

    private readonly List<Collider> enteredColliders = new();       // 충돌 처리한 콜라이더들 리스트

    private IObjectPool<GameObject> returnRef;                      // 반납 오브젝트 풀 주소

    private List<GameObject> executeHitEffects;                     // 실행할 타격/피격 이펙트 프리팹들 리스트
    private Renderer[] bulletRenderers;                             // 총알 외형 렌더러들 배열

    private BoxCollider bulletCollider;                             // 총알 콜라이더
    private Coroutine timerCoroutine;                               // 타이머 코루틴
    private WaitForSeconds returnTime;                              // 반납 시간

    private LayerMask ownerLayer;                                   // 소유자 레이어
    private Vector3 bulletSize;                                     // 총알 크기

    private bool startFire = false;                                 // 사격 시작 여부
    private bool isReturnedToPool;                                  // 풀 반환 완료 여부

    private float bulletSpeed;                                      // 총알 속도
    private float damage;                                           // 데미지
    private float cameraShakeValue;                                 // 카메라 흔들림 값
    
    private int penetrationCount;                                   // 관통 횟수
    #endregion

    public IReadOnlyList<GameObject> HitEffects => hitEffects;

    private void Awake()
    {
        // 총알 외형을 그려주는 렌더러들 받아오기
        bulletRenderers = GetComponentsInChildren<Renderer>();
        // 총알 콜라이더 받아오기
        bulletCollider = transform.GetComponent<BoxCollider>();
        // 총알 외형 받아오기
        var bulletLook = transform.GetChild(0);
        // 총알 콜라이더 위치에 맞게 외형 위치 수정
        transform.position = new Vector3(transform.position.x, transform.position.y,
                                                        -0.0125f * bulletLook.localScale.z);

        // 총알 콜라이더가 없다면
        if (bulletCollider == null)
        {
            // 총알 콜라이더 추가
            bulletCollider = gameObject.AddComponent<BoxCollider>();
            // 총알 콜라이더 크기 조절
            bulletCollider.size = new Vector3(0.01f * bulletLook.localScale.x,
                                                0.01f * bulletLook.localScale.y,
                                                    0.025f * bulletLook.localScale.z);
        }

        // 총알 콜라이더 크기 받아오기
        bulletSize = bulletCollider.size;
    }

    private void Update()
    {
        // 사격이 시작되지 않았다면
        if (!startFire)
            return;

        // 전방에 부딪힐 물체가 있다면
        if(Physics.Raycast(transform.position, transform.forward, out RaycastHit hit,
                                                    bulletSpeed * Time.deltaTime, ~ownerLayer))
            // 콜라이더 충돌 처리 시작
            EnterColliderProcess(hit.collider, hit.point);

        // 전방으로 총알 발사
        transform.position += bulletSpeed * Time.deltaTime * transform.forward;
    }

    private void OnTriggerEnter(Collider other)
    {
        // 사격이 시작되지 않았거나, 풀에 반환되었다면
        if (!startFire || isReturnedToPool)
            return;

        // 콜라이더 충돌 처리 시작
        EnterColliderProcess(other, other.ClosestPoint(transform.position));
    }

    // 총알 초기화
    private void OnDisable() => ResetBullet();

    /// <summary>
    /// 총알 초기화 함수
    /// </summary>
    private void ResetBullet()
    {
        // 타이머 코루틴이 비어있지 않다면
        if (timerCoroutine != null)
        {
            // 타이머 중지
            StopCoroutine(timerCoroutine);
            // 타이머 코루틴 초기화
            timerCoroutine = null;
        }

        // 하위 오브젝트가 있다면
        if (transform.childCount > 0)
        {
            // 하위 오브젝트들의 이펙트 실행기 인터페이스들 받아오기
            var executers = transform.GetComponentsInChildren<IEffectExecuter>();

            // 이펙트 실행기들의 수만큼
            foreach (var executer in executers)
                // 이펙트 종료 및 반납
                executer.StopEffect();
        }

        // 충돌 처리한 콜라이더들이 있다면
        if (enteredColliders.Count > 0)
            // 리스트 초기화
            enteredColliders.Clear();

        // 렌더러들이 있고 비어있지 않다면
        if (bulletRenderers != null && bulletRenderers.Length > 0)
            // 렌더러들의 수만큼
            foreach (var renderer in bulletRenderers)
                // 렌더러가 비활성화 되어있다면
                if(!renderer.enabled)
                    // 렌더러 활성화
                    renderer.enabled = true;

        // 콜라이더 크기 설정
        SetColliderSize();
    }

    /// <summary>
    /// 콜라이더 충돌 시 호출되는 함수
    /// </summary>
    /// <param name="other">충돌한 콜라이더</param>
    /// <param name="pos">충돌한 위치</param>
    private void EnterColliderProcess(Collider other, Vector3 pos)
    {
        // 충돌 처리한 콜라이더라면
        if (enteredColliders.Contains(other))
            return;

        // 충돌 처리한 콜라이더들 리스트에 추가
        enteredColliders.Add(other);

        // 통과할 수 있는 물체라면
        if (other.isTrigger)
            return;

        // 부딪힌 콜라이더를 포함한 상위 오브젝트에서, 데미지를 입을 수 있는 대상인지 받아오기
        var target = other.GetComponentInParent<IDamageable>();

        // 데미지를 입을 수 없는 물체라면
        if (target == null)
        {
            // 총알 위치를 부딪힌 위치로 수정
            transform.position = pos;
            // 총알 반납
            ReturnToPool();
            return;
        }

        // 부딪힌 물체의 레이어와 설정한 소유자 레이어와 같다면
        if (EqualsToOwnerLayer(other))
            return;

        // 부딪힌 대상에게 데미지 전달하기
        target.TakeDamage(damage);

        // 실행할 타격/피격 이펙트의 수만큼
        foreach(var hitEffect in executeHitEffects)
        {
            // 최대 이펙트 시간 저장할 변수
            float maxEffectTime = 0f;

            // 타격/피격 이펙트가 이펙트 스크립트를 가지고 있다면
            if(hitEffect.TryGetComponent<Effect>(out var effect))
                // 최대 이펙트 시간 받아오기
                maxEffectTime = effect.MaxEffectTime;

            // 타격/피격 이펙트 실행 후, 실행한 이펙트 받아오기
            var executeEffect = EffectManager.Instance.PlayEffect(hitEffect, pos,
                                            Quaternion.LookRotation(-transform.forward), maxEffectTime);

            // 실행한 이펙트가 타겟 이펙트 인터페이스를 가지고 있다면
            if (executeEffect.TryGetComponent<ITargetEffect>(out var targetEffect))
                // 따라다닐 대상 설정하기
                targetEffect.SetInfo(other.transform);
        }

        // 카메라 흔들림 값이 있다면
        if (cameraShakeValue > 0f)
            // 카메라 흔들림 이벤트 발행
            EventBus<CameraShakeEvent>.Publish(new CameraShakeEvent(cameraShakeValue));

        // 무한 관통이 아니라면
        if (penetrationCount != -1)
        {
            // 관통 횟수 감소
            penetrationCount = Math.Max(-1, penetrationCount - 1);

            // 관통 횟수가 남아있지 않다면
            if (penetrationCount <= -1)
            {
                // 총알 위치를 부딪힌 위치로 수정
                transform.position = pos;
                // 총알 반납
                ReturnToPool();
            }
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
        // 총알 초기화
        ResetBullet();
        // 총알 반납하기
        returnRef.Release(gameObject);
    }

    /// <summary>
    /// 레이어 비교 함수(레이어가 같으면 true 반환)
    /// </summary>
    /// <param name="hit">부딪힌 오브젝트</param>
    private bool EqualsToOwnerLayer(Collider hit) => ((1 << hit.gameObject.layer) & ownerLayer.value) != 0;

    /// <summary>
    /// 오브젝트 풀 주소 설정 함수
    /// </summary>
    /// <param name="poolRef">반환 주소</param>
    public void SetPoolRef(IObjectPool<GameObject> poolRef) => returnRef = poolRef;

    /// <summary>
    /// 콜라이더 크기 설정 함수
    /// </summary>
    /// <param name="size">콜라이더 크기(생략 가능, 기본값 : 총알 크기)</param>
    public void SetColliderSize(Vector3? size = null)
    {
        // 콜라이더 크기가 비어있고 콜라이더 크기가 총알 크기가 아니라면
        if (size == null && bulletCollider.size != bulletSize)
            // 콜라이더 크기를 총알 크기로 설정
            bulletCollider.size = bulletSize;
        // 콜라이더 크기가 있다면
        else if(size != null)
        {
            // 콜라이더 크기 받아오기
            Vector3 newSize = (Vector3)size;
            // 콜라이더 크기 설정(총알 크기보다 작을 경우, 총알 크기로)
            bulletCollider.size = new Vector3(newSize.x >= bulletSize.x ? newSize.x : bulletSize.x,
                                                newSize.y >= bulletSize.y ? newSize.y : bulletSize.y,
                                                    newSize.z >= bulletSize.z ? newSize.z : bulletSize.z);
        }
    }

    /// <summary>
    /// 사격 시작 함수
    /// </summary>
    /// <param name="spawnPoint">생성 장소</param>
    /// <param name="ownerLayer">소유자 레이어</param>
    /// <param name="damage">데미지</param>
    /// <param name="penetrationCount">관통 횟수(생략 가능, 기본값 : 0)</param>
    /// <param name="speed">총알 속도(생략 가능, 기본값 : 10f)</param>
    /// <param name="cameraShakeValue">카메라 흔들림 값(생략 가능, 기본값 : 0f)</param>
    /// <param name="effectPrefabs">실행할 타격/피격 이펙트 프리팹들(생략 가능, 기본값 : 총알에 설정된 이펙트)</param>
    public void StartFire(Transform spawnPoint, LayerMask ownerLayer, float damage,
                            int penetrationCount = 0, float speed = 30f, float cameraShakeValue = 0f,
                                                                    List<GameObject> effectPrefabs = null)
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
        SetInfo(ownerLayer, damage, penetrationCount, speed, cameraShakeValue, effectPrefabs);
        // 사격 시작
        startFire = true;
        // 풀 반환 완료 여부 초기화
        isReturnedToPool = false;
        // 타이머 시작
        timerCoroutine = StartCoroutine(ReturnRoutine());
    }

    /// <summary>
    /// 사격 시작 함수
    /// (위치/각도 설정이 필요가 없거나, 이미 위치/각도 설정을 했음)
    /// </summary>
    /// <param name="ownerLayer">소유자 레이어</param>
    /// <param name="damage">데미지</param>
    /// <param name="penetrationCount">관통 횟수(생략 가능, 기본값 : 0)</param>
    /// <param name="speed">총알 속도(생략 가능, 기본값 : 10f)</param>
    /// <param name="cameraShakeValue">카메라 흔들림 값(생략 가능, 기본값 : 0f)</param>
    /// <param name="effectPrefabs">실행할 타격/피격 이펙트 프리팹들(생략 가능, 기본값 : 총알에 설정된 이펙트)</param>
    public void StartFire(LayerMask ownerLayer, float damage,
                            int penetrationCount = 0, float speed = 10f, float cameraShakeValue = 0f,
                                                                    List<GameObject> effectPrefabs = null)
                    => StartFire(transform, ownerLayer, damage, penetrationCount, speed,
                                                            cameraShakeValue, effectPrefabs);

    /// <summary>
    /// 총알 정보 설정 함수
    /// </summary>
    /// <param name="layer">소유자 레이어</param>
    /// <param name="amount">데미지</param>
    /// <param name="count">관통 횟수</param>
    /// <param name="speed">총알 속도</param>
    /// <param name="effectPrefabs">실행할 타격/피격 이펙트 프리팹들</param>
    private void SetInfo(LayerMask layer, float amount, int count, float speed, float shakeValue,
                                                                            List<GameObject> effectPrefabs)
    {
        // 소유자 레이어 설정
        ownerLayer = layer;
        // 데미지 설정
        damage = amount;
        // 관통 횟수 설정
        penetrationCount = count;
        // 총알 속도 설정
        bulletSpeed = speed;
        // 최대 유지 시간 설정
        maxLifeTime = maxRange / bulletSpeed;
        // 반납 시간 설정
        returnTime = new WaitForSeconds(maxLifeTime);
        // 카메라 흔들림 값 설정
        cameraShakeValue = shakeValue;

        // 실행할 타격/피격 이펙트 프리팹들이 없거나, 비어있다면
        if (effectPrefabs == null || effectPrefabs.Count == 0)
            // 실행할 이펙트들을 총알의 타격/피격 이펙트로 설정
            executeHitEffects = hitEffects;
        // 실행할 타격/피격 이펙트 프리팹이 있다면
        else
            // 실행할 이펙트들 설정
            executeHitEffects = effectPrefabs;
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