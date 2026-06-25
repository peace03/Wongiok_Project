using UnityEngine;

[RequireComponent(typeof(PlayerStatus))]
public class PlayerAttack : MonoBehaviour
{
    [Header("References")]
    // 총알이 발사될 기준 위치입니다.
    [SerializeField] private Transform firePoint;

    // 원본 BulletFactory를 직접 참조해 풀링된 총알을 가져옵니다.
    [SerializeField] private BulletFactory bulletFactory;

    // Bullet 원본이 자기 소유 레이어 충돌을 무시할 때 사용하는 레이어입니다.
    [SerializeField] private LayerMask ownerLayer;

    // Bullet 원본에 넘길 관통 횟수입니다.
    [SerializeField] private int penetrationCount = 0;

    // 공격력 계산을 위한 플레이어 스탯 컴포넌트입니다.
    private PlayerStatus playerStatus;

    private void Awake()
    {
        // 실제 참조 캐싱은 PlayerInitializer에서 순서를 보장해 처리합니다.
    }

    public void Initialize(PlayerStatus status)
    {
        // 공격 데미지를 계산할 플레이어 스탯 참조를 초기화합니다.
        playerStatus = status != null ? status : GetComponent<PlayerStatus>();
    }

    public void Attack(Vector2 aimInput, bool isFacingRight, bool isGrounded)
    {
        // 필수 참조가 비어 있으면 공격을 중단합니다.
        if (firePoint == null || bulletFactory == null || playerStatus == null) return;

        // 입력과 바라보는 방향을 기준으로 공격 방향을 계산합니다.
        Vector3 attackDirection = GetAttackDirection(aimInput, isFacingRight, isGrounded);

        // 발사 시점의 공격력 최종값을 탄환 데미지로 사용합니다.
        float damage = playerStatus.GetAttackPower();

        // Bullet 원본은 origin.forward로 이동하므로 firePoint의 forward를 공격 방향에 맞춥니다.
        firePoint.rotation = Quaternion.FromToRotation(Vector3.forward, attackDirection.normalized);

        Bullet bullet = bulletFactory.GetBullet();
        if (bullet == null) return;

        // 풀에서 꺼낸 Bullet에 발사 기준점, 소유 레이어, 데미지, 관통 횟수를 넘깁니다.
        bullet.StartFire(firePoint, ownerLayer, damage, penetrationCount);

        // 공격 발사 이벤트를 발행해 사운드나 이펙트가 반응할 수 있게 합니다.
        EventBus<PlayerAttackFiredEvent>.Publish(
            new PlayerAttackFiredEvent(
                firePoint.position,
                attackDirection,
                damage
            )
        );
    }

    private Vector3 GetAttackDirection(Vector2 aimInput, bool isFacingRight, bool isGrounded)
    {
        float x = aimInput.x;
        float y = aimInput.y;

        // 지상에서 아래 입력만 들어온 경우에는 아래로 쏘지 않고 바라보는 방향으로 공격합니다.
        // 바닥을 향해 바로 발사되는 어색한 상황을 막기 위한 예외 처리입니다.
        if (isGrounded && Mathf.Approximately(x, 0f) && y < 0f)
        {
            return isFacingRight ? Vector3.left : Vector3.right;
        }

        // 아무 방향 입력이 없다면 마지막으로 바라보는 방향으로 공격합니다.
        if (Mathf.Approximately(x, 0f) && Mathf.Approximately(y, 0f))
        {
            return isFacingRight ? Vector3.left : Vector3.right;
        }

        // 수평 입력 없이 위아래 입력만 있다면 수직 방향으로 공격합니다.
        if (Mathf.Approximately(x, 0f))
        {
            return y > 0f ? Vector3.up : Vector3.down;
        }

        // 현재 게임 축 기준에 맞춰 입력 x를 월드 x축 반대 방향으로 매핑합니다.
        // 좌하단/우하단 입력은 대각선 아래 공격으로 쓰지 않고 수평 공격으로 보정합니다.
        if (y < 0f)
        {
            return new Vector3(-Mathf.Sign(x), 0f, 0f);
        }

        Vector3 direction = new Vector3(-x, y, 0f);
        return direction.normalized;
    }
}
