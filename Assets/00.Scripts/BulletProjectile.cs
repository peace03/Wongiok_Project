using UnityEngine;

// 플레이어와 몬스터가 공통으로 사용할 수 있는 단순 투사체입니다.
// 충돌이 확인되면 직접 HP를 깎지 않고 DamageRequestEvent를 발행합니다.
[RequireComponent(typeof(SphereCollider))]
public class BulletProjectile : MonoBehaviour
{
    [Header("Movement")]
    // 초당 이동 거리입니다. Init에서 받은 방향으로 이동합니다.
    [SerializeField] private float speed = 25f;

    // 충돌하지 않은 탄환이 씬에 계속 남지 않도록 자동 제거되는 시간입니다.
    [SerializeField] private float lifeTime = 3f;

    // 탄환이 맞출 수 있는 레이어입니다. 기본값은 모든 레이어입니다.
    [SerializeField] private LayerMask hitMask = ~0;

    [Header("Color")]
    // 플레이어가 지금 패링할 수 있는 탄환임을 보여줄 때 사용할 색입니다.
    [SerializeField] private Color parryReadyColor = Color.yellow;

    // Init에서 주입받은 이동 방향입니다.
    private Vector3 moveDirection;

    // 충돌 시 DamageRequestEvent에 실어 보낼 데미지입니다.
    private float damage;

    // Init 호출 전에는 움직이거나 판정하지 않도록 막습니다.
    private bool isInitialized;

    // 한 탄환이 여러 번 충돌 처리되지 않도록 막는 플래그입니다.
    private bool isHit;

    // 탄환을 발사한 주체입니다. 자기 자신과 자식 콜라이더는 피격 대상에서 제외합니다.
    private GameObject owner;

    // 탄환 자신의 콜라이더입니다. SphereCast 결과에서 자기 자신을 제외할 때 사용합니다.
    private Collider projectileCollider;

    // 이동 경로 검사에 사용할 구 반지름입니다.
    private float projectileRadius = 0.1f;

    // 투사체 색상 변경에 사용할 렌더러와 머티리얼 정보입니다.
    private Renderer[] targetRenderers;
    private Material[][] targetMaterials;
    private Color[][] originalColors;
    private string[][] colorProperties;

    // 같은 색상을 매 프레임 반복 적용하지 않기 위한 현재 표시 상태입니다.
    private bool isParryReadyVisualActive;

    private void Awake()
    {
        // 레이어 충돌이 꺼져 있어도 경로 검사를 할 수 있도록 자신의 콜라이더 정보를 저장합니다.
        projectileCollider = GetComponent<Collider>();
        projectileRadius = GetProjectileRadius(projectileCollider);

        CacheVisualMaterials();
    }

    public void Init(Vector3 direction, float attackDamage, GameObject attackOwner = null)
    {
        // 발사하는 쪽에서 방향, 데미지, 소유자를 주입합니다.
        moveDirection = direction.normalized;
        damage = attackDamage;
        owner = attackOwner;
        isInitialized = true;

        Destroy(gameObject, lifeTime);
    }

    public bool TryParry(GameObject parryOwner = null)
    {
        // 이미 충돌 처리된 투사체는 패링 성공으로 처리하지 않습니다.
        if (isHit) return false;

        isHit = true;
        owner = parryOwner;

        // 패링된 투사체는 DamageRequestEvent를 발행하지 않고 즉시 제거됩니다.
        Destroy(gameObject);
        return true;
    }

    public bool IsOwnedBy(GameObject candidate)
    {
        // 패링이나 외부 판정에서 자기 탄환을 제외할 때 사용합니다.
        if (candidate == null || owner == null) return false;

        return owner == candidate || owner.transform.IsChildOf(candidate.transform);
    }

    public void SetParryReadyVisual(bool isReady)
    {
        // 패링 가능 상태가 바뀔 때만 색상을 갱신합니다.
        if (isParryReadyVisualActive == isReady) return;

        isParryReadyVisualActive = isReady;

        if (isReady)
        {
            SetColor(parryReadyColor);
            return;
        }

        RestoreOriginalColors();
    }

    private void Update()
    {
        if (!isInitialized || isHit) return;

        // 프레임 이동량만큼 먼저 경로 검사를 하고, 맞은 대상이 없을 때만 위치를 갱신합니다.
        Vector3 movement = moveDirection * speed * Time.deltaTime;

        if (TryHitAlongMovement(movement))
            return;

        transform.position += movement;
    }

    private void OnTriggerEnter(Collider other)
    {
        // 레이어 충돌이 켜져 있는 대상은 Unity 트리거 이벤트로도 처리합니다.
        HandleHit(other, transform.position);
    }

    private bool TryHitAlongMovement(Vector3 movement)
    {
        // Player와 Bullet 레이어 충돌이 꺼져 있어도 맞았는지 확인하기 위한 수동 경로 검사입니다.
        float distance = movement.magnitude;
        if (distance <= Mathf.Epsilon) return false;

        RaycastHit[] hits = Physics.SphereCastAll(
            transform.position,
            projectileRadius,
            moveDirection,
            distance,
            hitMask,
            QueryTriggerInteraction.Collide
        );

        int closestHitIndex = -1;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hitCollider = hits[i].collider;

            // 자기 자신, 발사자, 마스크 밖 대상은 후보에서 제외합니다.
            if (ShouldIgnoreCollider(hitCollider)) continue;
            if (!IsInHitMask(hitCollider.gameObject)) continue;
            if (hits[i].distance >= closestDistance) continue;

            closestHitIndex = i;
            closestDistance = hits[i].distance;
        }

        if (closestHitIndex < 0) return false;

        RaycastHit hit = hits[closestHitIndex];

        // 실제 이펙트나 사운드 위치로 쓸 피격 지점을 계산합니다.
        Vector3 hitPoint = hit.collider.ClosestPoint(
            transform.position + moveDirection * hit.distance
        );

        transform.position = hitPoint;
        HandleHit(hit.collider, hitPoint);
        return true;
    }

    private void HandleHit(Collider other, Vector3 hitPoint)
    {
        // Trigger와 SphereCast가 같은 프레임에 들어와도 한 번만 처리합니다.
        if (isHit) return;
        if (ShouldIgnoreCollider(other)) return;
        if (!IsInHitMask(other.gameObject)) return;

        isHit = true;

        Debug.Log($"총알이 닿은 오브젝트: {other.gameObject.name} (Layer: {LayerMask.LayerToName(other.gameObject.layer)})");

        // 실제 데미지 적용은 Status 컴포넌트가 이 이벤트를 받아 처리합니다.
        EventBus<DamageRequestEvent>.Publish(
            new DamageRequestEvent(
                other.gameObject,
                other,
                owner,
                hitPoint,
                moveDirection,
                damage
            )
        );

        // 기존 사운드/이펙트 구독자가 계속 동작하도록 기존 총알 충돌 이벤트도 유지합니다.
        EventBus<PlayerBulletHitEvent>.Publish(
            new PlayerBulletHitEvent(
                other.gameObject,
                other,
                hitPoint,
                moveDirection,
                damage
            )
        );

        Destroy(gameObject);
    }

    private bool ShouldIgnoreCollider(Collider other)
    {
        // 탄환 자신이나 발사자 쪽 콜라이더는 맞은 것으로 처리하지 않습니다.
        if (other == null) return true;
        if (other == projectileCollider) return true;
        if (other.transform == transform || other.transform.IsChildOf(transform)) return true;

        return IsOwnerCollider(other);
    }

    private bool IsOwnerCollider(Collider other)
    {
        // owner의 자식 콜라이더까지 자기 공격으로 봅니다.
        if (owner == null) return false;

        return other.gameObject == owner || other.transform.IsChildOf(owner.transform);
    }

    private bool IsInHitMask(GameObject target)
    {
        // 인스펙터의 hitMask로 맞출 레이어를 제한합니다.
        return (hitMask.value & (1 << target.layer)) != 0;
    }

    private float GetProjectileRadius(Collider collider)
    {
        // SphereCast에 사용할 반지름을 현재 콜라이더 크기에서 계산합니다.
        if (collider == null) return projectileRadius;

        float maxScale = GetMaxAbsScale(transform.lossyScale);

        if (collider is SphereCollider sphereCollider)
            return Mathf.Max(0.01f, sphereCollider.radius * maxScale);

        if (collider is CapsuleCollider capsuleCollider)
            return Mathf.Max(0.01f, capsuleCollider.radius * maxScale);

        return Mathf.Max(0.01f, collider.bounds.extents.magnitude);
    }

    private float GetMaxAbsScale(Vector3 scale)
    {
        // 비균등 스케일에서도 충분히 큰 반지름으로 검사하기 위해 가장 큰 축을 사용합니다.
        return Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
    }

    private void CacheVisualMaterials()
    {
        // 패링 가능 표시를 위해 투사체와 자식 오브젝트의 머티리얼 색상 정보를 캐싱합니다.
        targetRenderers = GetComponentsInChildren<Renderer>();
        targetMaterials = new Material[targetRenderers.Length][];
        originalColors = new Color[targetRenderers.Length][];
        colorProperties = new string[targetRenderers.Length][];

        for (int i = 0; i < targetRenderers.Length; i++)
        {
            targetMaterials[i] = targetRenderers[i].materials;
            originalColors[i] = new Color[targetMaterials[i].Length];
            colorProperties[i] = new string[targetMaterials[i].Length];

            for (int j = 0; j < targetMaterials[i].Length; j++)
            {
                colorProperties[i][j] = GetColorProperty(targetMaterials[i][j]);
                if (string.IsNullOrEmpty(colorProperties[i][j])) continue;

                originalColors[i][j] = targetMaterials[i][j].GetColor(colorProperties[i][j]);
            }
        }
    }

    private void SetColor(Color color)
    {
        if (targetMaterials == null) return;

        for (int i = 0; i < targetMaterials.Length; i++)
        {
            for (int j = 0; j < targetMaterials[i].Length; j++)
            {
                if (string.IsNullOrEmpty(colorProperties[i][j])) continue;

                targetMaterials[i][j].SetColor(colorProperties[i][j], color);
            }
        }
    }

    private void RestoreOriginalColors()
    {
        if (targetMaterials == null || originalColors == null || colorProperties == null)
            return;

        for (int i = 0; i < targetMaterials.Length; i++)
        {
            for (int j = 0; j < targetMaterials[i].Length; j++)
            {
                if (string.IsNullOrEmpty(colorProperties[i][j])) continue;

                targetMaterials[i][j].SetColor(colorProperties[i][j], originalColors[i][j]);
            }
        }
    }

    private string GetColorProperty(Material material)
    {
        // URP는 _BaseColor, 기본 셰이더는 _Color를 주로 사용합니다.
        if (material == null) return string.Empty;
        if (material.HasProperty("_BaseColor")) return "_BaseColor";
        if (material.HasProperty("_Color")) return "_Color";

        return string.Empty;
    }
}
