using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("References")]
    // 총알을 발사할 기준 위치입니다.
    [SerializeField] private Transform firePoint;

    [SerializeField] private Transform muzzlePivot;

    [SerializeField] private GameObject shootingEffectPrefab;

    // 씬에 배치된 BulletFactory를 연결해 플레이어 총알을 가져옵니다.
    [SerializeField] private BulletFactory bulletFactory;

    // Bullet 원본이 자기 소유자 레이어 충돌을 무시할 때 사용하는 레이어입니다.
    [SerializeField] private LayerMask ownerLayer;

    // Bullet 원본에 넘길 관통 횟수입니다. 0이면 첫 명중 후 반환됩니다.
    [SerializeField] private int penetrationCount = 0;

    // 공격 데미지를 계산하기 위한 플레이어 스탯 컴포넌트입니다.
    private PlayerStatus playerStatus;
    private PlayerAnimatorDriver animatorDriver;
    private bool hasLoggedMissingFirePoint;
    private bool hasLoggedMissingBulletFactory;
    private bool hasLoggedMissingPlayerStatus;
    private bool hasLoggedEmptyOwnerLayer;

    public void Initialize(PlayerStatus status)
    {
        // 공격 데미지를 계산할 플레이어 스탯 참조를 초기화합니다.
        playerStatus = status != null ? status : GetComponent<PlayerStatus>();
        animatorDriver = GetComponent<PlayerAnimatorDriver>();
    }

    public void Attack(Vector2 aimInput, bool isFacingRight, bool isGrounded)
    {
        if (!CanAttackWithCurrentSettings()) return;

        // 입력과 바라보는 방향을 기준으로 공격 방향을 계산합니다.
        Vector3 attackDirection = GetAttackDirection(aimInput, isFacingRight, isGrounded);

        // 발사 시점의 최종 공격력을 총알 데미지로 사용합니다.
        float damage = playerStatus.GetAttackPower();

        // Bullet은 origin.forward로 이동하므로 firePoint의 forward를 공격 방향에 맞춥니다.
        UpdateMuzzleDirection(attackDirection, isFacingRight);

        Bullet bullet = bulletFactory.GetBullet();
        if (bullet == null) return;

        // 풀에서 꺼낸 Bullet에 발사 기준점, 소유자 레이어, 데미지, 관통 횟수를 넘깁니다.
        bullet.StartFire(firePoint, ownerLayer, damage, penetrationCount);
        animatorDriver?.PlayPistolShoot();
        PlayShootingEffect();

        // 발사 후처리 사운드나 이펙트가 반응할 수 있게 이벤트를 발행합니다.
        EventBus<PlayerAttackFiredEvent>.Publish(
            new PlayerAttackFiredEvent(
                firePoint.position,
                attackDirection,
                damage
            )
        );
    }

    private void UpdateMuzzleDirection(Vector3 attackDirection, bool isFacingRight)
    {
        Transform pivot = muzzlePivot != null ? muzzlePivot : firePoint;

        Vector3 localPosition = pivot.localPosition;
        localPosition.x = Mathf.Abs(localPosition.x) * (isFacingRight ? 1f : -1f);
        pivot.localPosition = localPosition;

        pivot.rotation = Quaternion.FromToRotation(Vector3.forward, attackDirection.normalized);
    }

    private void PlayShootingEffect()
    {
        if (shootingEffectPrefab == null) return;

        EventBus<EffectPlayData>.Publish(new EffectPlayData(
            shootingEffectPrefab,
            firePoint.position,
            firePoint.rotation,
            parent: firePoint));
    }

    private Vector3 GetAttackDirection(Vector2 aimInput, bool isFacingRight, bool isGrounded)
    {
        float x = aimInput.x;
        float y = aimInput.y;

        // 지상에서 아래 입력만 들어오면 아래로 쏘지 않고 바라보는 반대 방향으로 공격합니다.
        // 바닥을 향해 바로 발사하는 어색한 상황을 막기 위한 예외 처리입니다.
        // 방향 입력이 없으면 마지막으로 바라보는 방향으로 공격합니다.
        if (Mathf.Approximately(x, 0f) && Mathf.Approximately(y, 0f))
        {
            return isFacingRight ? Vector3.right : Vector3.left;
        }

        // 수평 입력 없이 위아래 입력만 있으면 수직 방향으로 공격합니다.
        if (Mathf.Approximately(x, 0f))
        {
            return new Vector3(isFacingRight ? 1f : -1f, Mathf.Sign(y), 0f).normalized;
        }

        // 좌하단/우하단 입력은 대각선 아래 공격으로 쓰지 않고 수평 공격으로 보정합니다.
        if (y < 0f)
        {
            return new Vector3(Mathf.Sign(x), -1f, 0f).normalized;
        }

        Vector3 direction = new Vector3(x, y, 0f);
        return direction.normalized;
    }
    #region 플레이어 경고 문구 ( 특정 오브젝트 or 스크립트 존재의 확인 )
    private bool CanAttackWithCurrentSettings()
    {
        bool canAttack = true;

        if (firePoint == null)
        {
            LogMissingSettingOnce(ref hasLoggedMissingFirePoint, "firePoint가 비어 있어 플레이어 공격을 실행할 수 없습니다.");
            canAttack = false;
        }

        if (bulletFactory == null)
        {
            LogMissingSettingOnce(ref hasLoggedMissingBulletFactory, "bulletFactory가 비어 있어 플레이어 총알을 가져올 수 없습니다.");
            canAttack = false;
        }

        if (playerStatus == null)
        {
            LogMissingSettingOnce(ref hasLoggedMissingPlayerStatus, "PlayerStatus 참조가 비어 있어 공격 데미지를 계산할 수 없습니다.");
            canAttack = false;
        }

        if (ownerLayer.value == 0)
        {
            LogMissingSettingOnce(ref hasLoggedEmptyOwnerLayer, "ownerLayer가 비어 있습니다. Player 레이어를 연결해야 자기 충돌을 안정적으로 무시할 수 있습니다.");
        }

        return canAttack;
    }
    #endregion

    private void LogMissingSettingOnce(ref bool hasLogged, string message)
    {
        if (hasLogged) return;

        Debug.LogWarning($"[PlayerAttack] {message}", this);
        hasLogged = true;
    }
}
