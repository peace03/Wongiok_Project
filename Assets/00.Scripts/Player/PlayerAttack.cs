using UnityEngine;

[RequireComponent(typeof(PlayerStatus))]
public class PlayerAttack : MonoBehaviour
{
    [Header("References")]
    // 총알이 생성될 위치입니다. 플레이어 총구나 손 위치에 배치합니다.
    [SerializeField] private Transform firePoint;

    // 발사할 총알 프리팹입니다.
    [SerializeField] private BulletProjectile bulletPrefab;

    // 공격력 스탯을 읽기 위한 플레이어 스탯 컴포넌트입니다.
    private PlayerStatus playerStatus;

    private void Awake()
    {
        playerStatus = GetComponent<PlayerStatus>();
    }

    public void Attack(Vector2 aimInput, bool isFacingRight, bool isGrounded)
    {
        // 필수 참조가 연결되어 있지 않으면 공격을 수행할 수 없습니다.
        if (firePoint == null || bulletPrefab == null) return;

        // 입력과 바라보는 방향을 기준으로 공격 방향을 계산합니다.
        Vector3 attackDirection = GetAttackDirection(aimInput, isFacingRight, isGrounded);

        // 발사 시점의 공격력 최종값을 피해량으로 사용합니다.
        float damage = playerStatus.GetAttackPower();

        // 총알 프리팹을 발사 위치에 생성합니다.
        BulletProjectile bullet = Instantiate(
            bulletPrefab,
            firePoint.position,
            Quaternion.identity
        );

        // 총알에 방향과 피해량을 주입해 이동을 시작시킵니다.
        bullet.Init(attackDirection, damage, gameObject);

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

        // 지상에서 아래 입력만 누른 경우에는 아래로 쏘지 않고 바라보는 방향으로 공격합니다.
        // 바닥을 향해 바로 발사되는 어색한 상황을 막기 위한 예외 처리입니다.
        if (isGrounded && Mathf.Approximately(x, 0f) && y < 0f)
        {
            return isFacingRight ? Vector3.left : Vector3.right;
        }

        // 아무 방향 입력이 없다면 마지막으로 바라본 방향으로 공격합니다.
        if (Mathf.Approximately(x, 0f) && Mathf.Approximately(y, 0f))
        {
            return isFacingRight ? Vector3.left : Vector3.right;
        }

        // 수평 입력 없이 위/아래 입력만 있다면 수직 방향으로 공격합니다.
        if (Mathf.Approximately(x, 0f))
        {
            return y > 0f ? Vector3.up : Vector3.down;
        }

        // 현재 게임 축 기준으로 입력 x는 z축 방향에 매핑합니다.
        // x가 양수일 때 Vector3.back 방향이 되도록 -x를 사용합니다.
        //Vector3 direction = new Vector3(0f, y, -x);
        Vector3 direction = new Vector3(-x, y, 0f);
        return direction.normalized;
    }
}
