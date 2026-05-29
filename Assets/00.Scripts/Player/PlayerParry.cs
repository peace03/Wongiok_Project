using System.Collections.Generic;
using UnityEngine;

// 플레이어의 즉시 패링 판정을 담당하는 컴포넌트입니다.
// v1에서는 주변 범위 안에 들어온 BulletProjectile만 제거하고 데미지 요청은 발생시키지 않습니다.
public class PlayerParry : MonoBehaviour
{
    // 기본 패링 대상 레이어입니다. 플레이어 탄환이 패링되지 않도록 몬스터 투사체만 봅니다.
    private const string DefaultParryLayerName = "MonsterProjectile";

    [Header("Parry")]
    // 패링으로 감지할 반경입니다. 플레이어 주변에 들어온 투사체만 대상으로 봅니다.
    [SerializeField] private float parryRadius = 1.5f;

    // 패링 대상으로 검사할 레이어입니다. 기본값은 MonsterProjectile 레이어입니다.
    [SerializeField] private LayerMask parryMask;

    // 현재 플레이어 상태에서 패링 가능한지 확인하기 위한 컨트롤러 참조입니다.
    private PlayerController playerController;

    // 이전 프레임에 패링 가능 표시가 켜졌던 투사체 목록입니다.
    private readonly List<BulletProjectile> parryReadyProjectiles = new List<BulletProjectile>();

    // 이번 프레임에 패링 가능 범위 안에서 감지된 투사체 목록입니다.
    private readonly List<BulletProjectile> detectedProjectiles = new List<BulletProjectile>();

    private void Awake()
    {
        // 런타임에 자동 추가된 컴포넌트도 올바른 패링 레이어만 보도록 보정합니다.
        playerController = GetComponent<PlayerController>();
        EnsureDefaultParryMask();
    }

    private void OnValidate()
    {
        // 에디터에서 새로 붙인 컴포넌트도 기본 패링 마스크를 자동으로 맞춥니다.
        EnsureDefaultParryMask();
    }

    private void Update()
    {
        // 패링 판정 가능한 투사체를 매 프레임 노란색으로 표시합니다.
        RefreshParryReadyVisuals();
    }

    private void OnDisable()
    {
        // 플레이어가 비활성화될 때 탄환 색상이 노란색으로 남지 않도록 원래 색으로 돌립니다.
        ClearParryReadyVisuals();
    }

    public bool TryParry()
    {
        EnsureDefaultParryMask();

        BulletProjectile closestProjectile = FindClosestParryProjectile();
        if (closestProjectile == null) return false;

        // 투사체가 이미 충돌 처리된 상태가 아니라면 제거하고 성공 로그를 남깁니다.
        if (!closestProjectile.TryParry(gameObject)) return false;

        Debug.Log("패링성공 !!");
        return true;
    }

    private void RefreshParryReadyVisuals()
    {
        detectedProjectiles.Clear();

        if (CanUseParry())
            CollectParryProjectiles(detectedProjectiles);

        for (int i = 0; i < parryReadyProjectiles.Count; i++)
        {
            BulletProjectile projectile = parryReadyProjectiles[i];
            if (projectile == null) continue;
            if (detectedProjectiles.Contains(projectile)) continue;

            projectile.SetParryReadyVisual(false);
        }

        for (int i = 0; i < detectedProjectiles.Count; i++)
        {
            BulletProjectile projectile = detectedProjectiles[i];
            if (projectile == null) continue;

            projectile.SetParryReadyVisual(true);
        }

        parryReadyProjectiles.Clear();
        parryReadyProjectiles.AddRange(detectedProjectiles);
    }

    private void ClearParryReadyVisuals()
    {
        for (int i = 0; i < parryReadyProjectiles.Count; i++)
        {
            if (parryReadyProjectiles[i] == null) continue;

            parryReadyProjectiles[i].SetParryReadyVisual(false);
        }

        parryReadyProjectiles.Clear();
        detectedProjectiles.Clear();
    }

    private BulletProjectile FindClosestParryProjectile()
    {
        // 현재 패링 범위 안의 투사체 중 가장 가까운 투사체 하나를 찾습니다.
        BulletProjectile closestProjectile = null;
        float closestDistanceSqr = float.MaxValue;

        detectedProjectiles.Clear();
        CollectParryProjectiles(detectedProjectiles);

        for (int i = 0; i < detectedProjectiles.Count; i++)
        {
            BulletProjectile projectile = detectedProjectiles[i];
            if (projectile == null) continue;

            float distanceSqr = (projectile.transform.position - transform.position).sqrMagnitude;
            if (distanceSqr >= closestDistanceSqr) continue;

            closestProjectile = projectile;
            closestDistanceSqr = distanceSqr;
        }

        return closestProjectile;
    }

    private void CollectParryProjectiles(List<BulletProjectile> results)
    {
        // 현재 패링 범위 안에 있는 패링 대상 투사체들을 수집합니다.
        Collider[] hits = Physics.OverlapSphere(
            GetParryCenter(),
            parryRadius,
            parryMask,
            QueryTriggerInteraction.Collide
        );

        for (int i = 0; i < hits.Length; i++)
        {
            BulletProjectile projectile = hits[i].GetComponentInParent<BulletProjectile>();
            if (projectile == null) continue;
            if (projectile.IsOwnedBy(gameObject)) continue;
            if (results.Contains(projectile)) continue;

            results.Add(projectile);
        }
    }

    private bool CanUseParry()
    {
        // 컨트롤러가 아직 없거나 현재 상태가 패링을 허용할 때만 패링 가능 표시를 켭니다.
        return playerController == null || playerController.CanParry;
    }

    private void EnsureDefaultParryMask()
    {
        // 이미 사용자가 별도 마스크를 지정했다면 그 값을 유지합니다.
        if (parryMask.value != 0 && parryMask.value != ~0) return;

        int monsterProjectileMask = LayerMask.GetMask(DefaultParryLayerName);
        if (monsterProjectileMask == 0) return;

        parryMask = monsterProjectileMask;
    }

    private Vector3 GetParryCenter()
    {
        // 플레이어 기준 위치보다 반지름의 절반만큼 위로 올려 상체 쪽 패링 판정을 넓게 봅니다.
        return transform.position + Vector3.up * (parryRadius * 0.5f);
    }

    private void OnDrawGizmos()
    {
        // 에디터에서 플레이어 선택 여부와 관계없이 패링 범위를 확인하기 위한 디버그 표시입니다.
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(GetParryCenter(), parryRadius);
    }
}
