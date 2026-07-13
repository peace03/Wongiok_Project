//using System;
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.Pool;

//public class Bullet : MonoBehaviour, IPoolable
//{
//    [Header("총알 스탯")]
//    [Header("소유자 레이어")]
//    [Tooltip("총알을 발사한 주체의 레이어를 저장하여, 부딪힌 물체를 구분할 예정임")]
//    [SerializeField] private LayerMask ownerLayer;          // 소유자 레이어
//    [Header("데미지")]
//    [SerializeField] private float damage = 0f;             // 데미지
//    [Header("관통 횟수")]
//    [Tooltip("-1로 두면 무한 관통, 0으로 두면 관통 0회, 1로 두면 관통 1회 스킬이 됨")]
//    [SerializeField] private int penetrationCount = 0;      // 관통 횟수
//    [Header("속도")]
//    [Tooltip("날아가는 (임시)속도, 얼마든지 조정하셔도 됨")]
//    [SerializeField] private float speed = 10f;             // 속도
//    [Header("지속 시간")]
//    [Tooltip("날아가는 총알이 유지되는 시간, 얼마든지 조장하셔도 됨")]
//    [SerializeField] private float duration = 5f;           // 지속 시간

//    private IObjectPool<GameObject> returnRef;              // 반납 오브젝트 풀 주소

//    private Coroutine timerCoroutine;                       // 타이머 코루틴
//    private WaitForSeconds returnTime;                      // 반납 시간

//    private bool startFire = false;                         // 사격 시작 여부

//    private void Update()
//    {
//        // 사격이 시작되지 않았다면
//        if (!startFire)
//            return;

//        // 전방으로 총알 발사
//        transform.position += speed * Time.deltaTime * transform.forward;
//    }

//    private void OnTriggerEnter(Collider other)
//    {
//        // 닿은 물체의 레이어가 소유자 레이어와 같다면
//        if (((1 << other.gameObject.layer) & ownerLayer.value) != 0)
//            return;
//        // 데미지를 입을 수 없는 물체라면
//        else if (!other.TryGetComponent<IDamageable>(out var target))
//            // 총알 반납
//            returnRef.Release(gameObject);
//        else
//        {
//            // 데미지 전달
//            target.TakeDamage(damage);

//            // 무한 관통이 아니라면
//            if (penetrationCount != -1)
//            {
//                // 관통 횟수 감소
//                penetrationCount = Math.Max(-1, penetrationCount - 1);

//                // 관통 횟수가 남아있지 않다면
//                if (penetrationCount <= -1)
//                    // 총알 반납
//                    returnRef.Release(gameObject);
//            }
//        }
//    }

//    private void OnDisable()
//    {
//        // 타이머 코루틴이 비어있지 않다면
//        if(timerCoroutine != null)
//        {
//            // 타이머 중지
//            StopCoroutine(timerCoroutine);
//            // 타이머 코루틴 초기화
//            timerCoroutine = null;
//        }
//    }

//    /// <summary>
//    /// 오브젝트 풀 주소 설정 함수(반환 주소 설정)
//    /// </summary>
//    public void SetPoolRef(IObjectPool<GameObject> poolRef) => returnRef = poolRef;

//    /// <summary>
//    /// 사격 시작 함수
//    /// </summary>
//    public void StartFire(Transform origin, LayerMask ownerLayer, float damage,
//                                                            int penetrationCount = 0)
//    {
//        // 타이머 코루틴이 비어있지 않다면
//        if (timerCoroutine != null)
//            // 타이머 중지
//            StopCoroutine(timerCoroutine);

//        // 위치, 각도 설정
//        transform.SetPositionAndRotation(origin.position, origin.rotation);
//        // 총알 정보 설정
//        SetInfo(ownerLayer, damage, penetrationCount);
//        // 사격 시작
//        startFire = true;
//        // 반납 시간 초기화
//        returnTime = new WaitForSeconds(duration);
//        // 타이머 시작
//        timerCoroutine = StartCoroutine(ReturnRoutine());
//    }

//    /// <summary>
//    /// 총알 정보 설정 함수
//    /// </summary>
//    private void SetInfo(LayerMask layer, float amount, int count)
//    {
//        // 소유자 레이어 설정
//        ownerLayer = layer;
//        // 데미지 설정
//        damage = amount;
//        // 관통 횟수 설정
//        penetrationCount = count;
//    }

//    /// <summary>
//    /// 타이머 코루틴 함수
//    /// </summary>
//    private IEnumerator ReturnRoutine()
//    {
//        // 반납 시간 기다리기
//        yield return returnTime;
//        // 사격 종료
//        startFire = false;
//        // 타이머 코루틴 초기화
//        timerCoroutine = null;
//        // 총알 반납
//        returnRef.Release(gameObject);
//    }
//}



using UnityEngine;
using UnityEngine.Pool;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class Bullet : MonoBehaviour, IPoolable
{
    [Header("Owner")]
    [Tooltip("총알을 발사한 주체의 레이어입니다")]
    [SerializeField] private LayerMask ownerLayer;

    [Header("Hit Filter")]
    [Tooltip("데미지를 줄 수 있는 대상 레이어입니다")]
    [SerializeField] private LayerMask damageMask;

    [Tooltip("총알을 막는 지형 또는 장애물 레이어입니다")]
    [SerializeField] private LayerMask blockMask;

    [Header("Damage")]
    [SerializeField] private float damage = 0f;

    [Header("Penetration")]
    [Tooltip("-1은 무한 관통, 0은 관통 없음, 1은 한 대상을 관통합니다")]
    [SerializeField] private int penetrationCount = 0;

    [Header("Movement")]
    [SerializeField] private float speed = 10f;
    [SerializeField] private float duration = 5f;
    [SerializeField] private float sphereCastRadius = 0.08f;

    private IObjectPool<GameObject> returnRef;
    private Coroutine returnCoroutine;
    private WaitForSeconds waitTime;
    private bool startFire;
    private bool isReturning;
    private Transform ownerRoot;
    private Collider bulletCollider;
    private Rigidbody bulletRigidbody;
    private readonly List<Object> damagedTargets = new List<Object>(8);

    // 총알에 필요한 물리 설정을 준비합니다
    private void Awake()
    {
        PreparePhysicsComponents();
    }

    // 인스펙터 값이 잘못 들어갔을 때 최소 값을 보장합니다
    private void OnValidate()
    {
        speed = Mathf.Max(0f, speed);
        duration = Mathf.Max(0.01f, duration);
        sphereCastRadius = Mathf.Max(0.01f, sphereCastRadius);
        damage = Mathf.Max(0f, damage);
    }

    // 사격 중인 총알의 이동과 이동 경로 충돌 검사를 처리합니다
    private void Update()
    {
        if (!startFire || isReturning)
        {
            return;
        }

        Vector3 movement = transform.forward * speed * Time.deltaTime;

        if (TryHitAlongMovement(movement))
        {
            return;
        }

        transform.position += movement;
    }

    // Trigger Collider에 닿았을 때 충돌 처리를 시도합니다
    private void OnTriggerEnter(Collider other)
    {
        HandleHit(other, transform.position);
    }

    // 비활성화될 때 반납 코루틴과 상태를 정리합니다
    private void OnDisable()
    {
        StopReturnCoroutine();
        startFire = false;
        isReturning = false;
        damagedTargets.Clear();
    }

    // 오브젝트 풀 주소를 설정합니다
    public void SetPoolRef(IObjectPool<GameObject> poolRef)
    {
        returnRef = poolRef;
    }

    // 발사 위치와 방향과 데미지 정보를 설정하고 총알을 발사합니다
    public void StartFire(Transform origin, LayerMask ownerLayer, float damage, int penetrationCount = 0)
    {
        StopReturnCoroutine();
        PreparePhysicsComponents();

        if (origin == null)
        {
            ReleaseToPool();
            return;
        }

        transform.SetPositionAndRotation(origin.position, origin.rotation);

        SetInfo(ownerLayer, damage, penetrationCount);
        ownerRoot = origin.root;
        startFire = true;
        isReturning = false;
        damagedTargets.Clear();

        waitTime = new WaitForSeconds(duration);
        returnCoroutine = StartCoroutine(ReturnRoutine());
    }

    // 총알 실행 정보를 저장합니다
    private void SetInfo(LayerMask layer, float amount, int count)
    {
        ownerLayer = layer;
        damage = amount;
        penetrationCount = count;
    }

    // Collider와 Rigidbody를 총알용 Trigger 설정으로 보정합니다
    private void PreparePhysicsComponents()
    {
        bulletCollider = GetComponent<Collider>();

        if (bulletCollider == null)
        {
            SphereCollider sphereCollider = gameObject.AddComponent<SphereCollider>();
            sphereCollider.radius = Mathf.Max(0.05f, sphereCastRadius);
            bulletCollider = sphereCollider;
        }

        bulletCollider.isTrigger = true;

        bulletRigidbody = GetComponent<Rigidbody>();

        if (bulletRigidbody == null)
        {
            bulletRigidbody = gameObject.AddComponent<Rigidbody>();
        }

        bulletRigidbody.isKinematic = true;
        bulletRigidbody.useGravity = false;
        bulletRigidbody.detectCollisions = true;
        bulletRigidbody.constraints = RigidbodyConstraints.FreezeRotation;
    }

    // 빠른 총알이 프레임 사이에 대상을 지나치지 않도록 이동 경로를 검사합니다
    private bool TryHitAlongMovement(Vector3 movement)
    {
        float distance = movement.magnitude;

        if (distance <= Mathf.Epsilon)
        {
            return false;
        }

        int combinedMask = damageMask.value | blockMask.value;

        if (combinedMask == 0)
        {
            return false;
        }

        RaycastHit[] hits = Physics.SphereCastAll(
            transform.position,
            sphereCastRadius,
            transform.forward,
            distance,
            combinedMask,
            QueryTriggerInteraction.Collide
        );

        if (hits == null || hits.Length == 0)
        {
            return false;
        }

        System.Array.Sort(hits, CompareRaycastHitDistance);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hitCollider = hits[i].collider;

            if (ShouldIgnoreCollider(hitCollider))
            {
                continue;
            }

            if (!IsInLayerMask(hitCollider.gameObject, damageMask) && !IsInLayerMask(hitCollider.gameObject, blockMask))
            {
                continue;
            }

            Vector3 hitPoint = transform.position + transform.forward * hits[i].distance;
            transform.position = hitPoint;

            bool consumed = HandleHit(hitCollider, hitPoint);

            if (consumed)
            {
                return true;
            }
        }

        return false;
    }

    // 두 충돌 정보의 거리를 비교합니다
    private int CompareRaycastHitDistance(RaycastHit a, RaycastHit b)
    {
        return a.distance.CompareTo(b.distance);
    }

    // 충돌한 대상이 데미지 대상인지 차단 대상인지 판정합니다
    private bool HandleHit(Collider other, Vector3 hitPoint)
    {
        if (isReturning)
        {
            return true;
        }

        if (ShouldIgnoreCollider(other))
        {
            return false;
        }

        if (IsInLayerMask(other.gameObject, damageMask))
        {
            bool damaged = TryDamageTarget(other, hitPoint);

            if (damaged)
            {
                return true;
            }
        }

        if (IsInLayerMask(other.gameObject, blockMask))
        {
            ReleaseToPool();
            return true;
        }

        return false;
    }

    // 데미지를 받을 수 있는 대상에게 데미지만 전달합니다
    private bool TryDamageTarget(Collider other, Vector3 hitPoint)
    {
        IDamageable target = DamageableResolver.FindDamageable(other);

        if (target == null || !target.CanTakeDamage)
        {
            return false;
        }

        Object targetKey = DamageableResolver.GetTargetKey(other, target);

        if (targetKey != null && damagedTargets.Contains(targetKey))
        {
            return false;
        }

        if (targetKey != null)
        {
            damagedTargets.Add(targetKey);
        }

        target.TakeDamage(damage);
        PublishDamageHitEvent(other, target, hitPoint);

        return ConsumePenetrationOrRelease();
    }

    // 무시해야 하는 Collider인지 확인합니다
    private bool ShouldIgnoreCollider(Collider other)
    {
        if (other == null)
        {
            return true;
        }

        if (bulletCollider != null && other == bulletCollider)
        {
            return true;
        }

        if (other.transform == transform || other.transform.IsChildOf(transform))
        {
            return true;
        }

        if (ownerRoot != null)
        {
            if (other.transform == ownerRoot || other.transform.IsChildOf(ownerRoot))
            {
                return true;
            }
        }

        if (((1 << other.gameObject.layer) & ownerLayer.value) != 0)
        {
            return true;
        }

        return false;
    }

    // 대상 오브젝트가 지정한 레이어 마스크에 포함되는지 확인합니다
    private bool IsInLayerMask(GameObject targetObject, LayerMask layerMask)
    {
        if (targetObject == null)
        {
            return false;
        }

        return ((1 << targetObject.layer) & layerMask.value) != 0;
    }

    // 데미지 적용 사실을 이벤트 버스로 알립니다
    private void PublishDamageHitEvent(Collider hitCollider, IDamageable target, Vector3 hitPoint)
    {
        GameObject targetObject = DamageableResolver.GetTargetObject(hitCollider, target);
        Vector3 hitDirection = transform.forward;

        DamageHitEvent hitEvent = new DamageHitEvent(
            targetObject,
            gameObject,
            hitCollider,
            hitPoint,
            hitDirection,
            damage
        );

        EventBus<DamageHitEvent>.Publish(hitEvent);
    }

    // 관통 횟수를 소모하고 총알 소비 여부를 반환합니다
    private bool ConsumePenetrationOrRelease()
    {
        if (penetrationCount == -1)
        {
            return false;
        }

        if (penetrationCount > 0)
        {
            penetrationCount--;
            return false;
        }

        ReleaseToPool();
        return true;
    }

    // 지정된 지속 시간이 끝나면 총알을 풀로 반납합니다
    private IEnumerator ReturnRoutine()
    {
        yield return waitTime;
        returnCoroutine = null;
        ReleaseToPool();
    }

    // 실행 중인 반납 코루틴을 중지합니다
    private void StopReturnCoroutine()
    {
        if (returnCoroutine == null)
        {
            return;
        }

        StopCoroutine(returnCoroutine);
        returnCoroutine = null;
    }

    // 총알을 오브젝트 풀로 반납합니다
    private void ReleaseToPool()
    {
        if (isReturning)
        {
            return;
        }

        isReturning = true;
        startFire = false;
        StopReturnCoroutine();

        if (returnRef != null)
        {
            returnRef.Release(gameObject);
            return;
        }

        gameObject.SetActive(false);
    }
}