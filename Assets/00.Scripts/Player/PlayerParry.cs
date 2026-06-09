using System.Collections.Generic;
using UnityEngine;

// 플레이어 주변의 패리 가능 투사체를 찾고, 입력 시 가장 가까운 투사체를 패리합니다.
public class PlayerParry : MonoBehaviour
{
    // 인스펙터에서 별도 마스크가 없으면 몬스터 투사체 레이어만 검사합니다.
    private const string DefaultParryLayerName = "MonsterProjectile";

    [Header("Parry")]
    // 플레이어 주변에서 패리 가능한 투사체를 찾는 반경입니다.
    [SerializeField] private float parryRadius = 1.5f;
    [SerializeField] private LayerMask parryMask;

    private PlayerController playerController;

    // 직전 프레임에 패리 가능 표시를 켜 둔 투사체 목록입니다.
    private readonly List<IParryableProjectile> parryReadyProjectiles = new List<IParryableProjectile>();

    // 이번 프레임에 새로 감지한 투사체 목록입니다.
    private readonly List<IParryableProjectile> detectedProjectiles = new List<IParryableProjectile>();

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        EnsureDefaultParryMask();
    }

    private void OnValidate()
    {
        EnsureDefaultParryMask();
    }

    private void Update()
    {
        RefreshParryReadyVisuals();
    }

    private void OnDisable()
    {
        ClearParryReadyVisuals();
    }

    public bool TryParry()
    {
        EnsureDefaultParryMask();

        // 범위 안의 투사체 중 가장 가까운 대상 하나만 패리합니다.
        IParryableProjectile closestProjectile = FindClosestParryProjectile();
        if (IsMissing(closestProjectile)) return false;

        if (!closestProjectile.TryParry(gameObject)) return false;

        Debug.Log("패링 성공");
        return true;
    }

    private void RefreshParryReadyVisuals()
    {
        // 이번 프레임 감지 결과와 직전 표시 목록을 비교해 필요한 대상만 색을 갱신합니다.
        detectedProjectiles.Clear();

        if (CanUseParry())
            CollectParryProjectiles(detectedProjectiles);

        for (int i = 0; i < parryReadyProjectiles.Count; i++)
        {
            IParryableProjectile projectile = parryReadyProjectiles[i];
            if (IsMissing(projectile)) continue;
            if (detectedProjectiles.Contains(projectile)) continue;

            projectile.SetParryReadyVisual(false);
        }

        for (int i = 0; i < detectedProjectiles.Count; i++)
        {
            IParryableProjectile projectile = detectedProjectiles[i];
            if (IsMissing(projectile)) continue;

            projectile.SetParryReadyVisual(true);
        }

        parryReadyProjectiles.Clear();
        parryReadyProjectiles.AddRange(detectedProjectiles);
    }

    private void ClearParryReadyVisuals()
    {
        for (int i = 0; i < parryReadyProjectiles.Count; i++)
        {
            IParryableProjectile projectile = parryReadyProjectiles[i];
            if (IsMissing(projectile)) continue;

            projectile.SetParryReadyVisual(false);
        }

        parryReadyProjectiles.Clear();
        detectedProjectiles.Clear();
    }

    private IParryableProjectile FindClosestParryProjectile()
    {
        IParryableProjectile closestProjectile = null;
        float closestDistanceSqr = float.MaxValue;

        detectedProjectiles.Clear();
        CollectParryProjectiles(detectedProjectiles);

        for (int i = 0; i < detectedProjectiles.Count; i++)
        {
            IParryableProjectile projectile = detectedProjectiles[i];
            if (IsMissing(projectile)) continue;

            float distanceSqr = (projectile.Position - transform.position).sqrMagnitude;
            if (distanceSqr >= closestDistanceSqr) continue;

            closestProjectile = projectile;
            closestDistanceSqr = distanceSqr;
        }

        return closestProjectile;
    }

    private void CollectParryProjectiles(List<IParryableProjectile> results)
    {
        // 레이어로 1차 필터링한 뒤, IParryableProjectile 구현체만 패리 대상으로 수집합니다.
        Collider[] hits = Physics.OverlapSphere(
            GetParryCenter(),
            parryRadius,
            parryMask,
            QueryTriggerInteraction.Collide
        );

        for (int i = 0; i < hits.Length; i++)
        {
            IParryableProjectile projectile = GetParryableProjectile(hits[i]);
            if (IsMissing(projectile)) continue;
            if (projectile.IsOwnedBy(gameObject)) continue;
            if (results.Contains(projectile)) continue;

            results.Add(projectile);
        }
    }

    private IParryableProjectile GetParryableProjectile(Collider hit)
    {
        if (hit == null) return null;

        // 인터페이스는 GetComponentInParent<T> 제네릭 제약에 걸릴 수 있어 MonoBehaviour를 순회합니다.
        MonoBehaviour[] behaviours = hit.GetComponentsInParent<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is IParryableProjectile projectile)
                return projectile;
        }

        return null;
    }

    private bool CanUseParry()
    {
        return playerController == null || playerController.CanParry;
    }

    private bool IsMissing(IParryableProjectile projectile)
    {
        // Unity Object가 이미 Destroy된 경우 인터페이스 참조만 남을 수 있어 함께 검사합니다.
        if (projectile == null) return true;

        return projectile is Object unityObject && unityObject == null;
    }

    private void EnsureDefaultParryMask()
    {
        // 사용자가 직접 패리 마스크를 지정했다면 자동 보정하지 않습니다.
        if (parryMask.value != 0 && parryMask.value != ~0) return;

        int monsterProjectileMask = LayerMask.GetMask(DefaultParryLayerName);
        if (monsterProjectileMask == 0) return;

        parryMask = monsterProjectileMask;
    }

    private Vector3 GetParryCenter()
    {
        // 발밑보다 몸통 주변을 검사하도록 반경의 절반만큼 위로 올립니다.
        return transform.position + Vector3.up * (parryRadius * 0.5f);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(GetParryCenter(), parryRadius);
    }
}
