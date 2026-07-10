using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerStatus))]
public class PlayerMovement : MonoBehaviour
{
    // 실제 이동을 수행하는 Unity CharacterController입니다.
    private CharacterController cc;

    // 이동 속도, 점프력, 최대 점프 횟수 같은 스탯 값을 가져오기 위한 참조입니다.
    private PlayerStatus playerStatus;

    [Header("Gravity")]
    // 매 프레임 수직 속도에 더해지는 중력 값입니다. 음수일수록 더 빠르게 떨어집니다.
    [SerializeField] private float gravity = -20f;

    [Header("Dash")]
    // 한 번 대쉬할 때 이동할 전체 거리입니다.
    [SerializeField] private float dashDistance = 4f;

    // 대쉬가 지속되는 시간입니다.
    [SerializeField] private float dashDuration = 0.18f;

    // 다음 대쉬를 다시 사용할 수 있기까지의 대기 시간입니다.
    [SerializeField] private float dashCooldown = 2f;

    // 현재까지 사용한 점프 횟수입니다. 착지하면 0으로 초기화됩니다.
    private int jumpCount;

    // 이전 프레임에 바닥에 있었는지 저장해서 착지 순간을 감지합니다.
    private bool wasGrounded;

    // 공중 대쉬를 아직 사용할 수 있는지 저장합니다. 착지하면 다시 true가 됩니다.
    private bool canAirDash = true;

    // 마지막 대쉬 시간입니다. 쿨타임 계산에 사용합니다.
    private float lastDashTime = -999f;

    // 수직 속도 중심의 이동 속도입니다. 점프와 중력이 이 값의 y를 갱신합니다.
    private Vector3 velocity;

    // 현재 바닥에 닿아 있는지 CharacterController 기준으로 반환합니다.
    public bool IsGrounded => cc.isGrounded;

    // 수직 속도가 내려가는 중인지 판단합니다. 착지 상태 전환에 사용합니다.
    public bool IsFalling => velocity.y <= 0f;

    // 대쉬 상태가 참조하는 대쉬 지속 시간입니다.
    public float DashDuration => dashDuration;

    // 대쉬 거리와 시간을 기반으로 초당 대쉬 속도를 계산합니다.
    public float DashSpeed => dashDistance / dashDuration;

    private void Awake()
    {
        // 실제 참조 캐싱은 PlayerInitializer에서 순서를 보장해 처리합니다.
    }

    public void Initialize(PlayerStatus status)
    {
        // 이동 계산에 필요한 컨트롤러와 스탯 참조를 초기화합니다.
        cc = GetComponent<CharacterController>();
        playerStatus = status != null ? status : GetComponent<PlayerStatus>();

        // 시작 시점의 바닥 상태를 저장해 첫 중력 처리에서 착지 판정이 꼬이지 않게 합니다.
        wasGrounded = cc.isGrounded;
    }

    public void Move(Vector2 input)
    {
        // 수평 이동과 수직 이동을 한 프레임 안에서 순서대로 적용합니다.
        MoveHorizontal(input);
        MoveVerticalVelocity();
    }

    public void MoveHorizontal(Vector2 input)
    {
        float moveSpeed = playerStatus.GetMoveSpeed();

        // 현재 게임 축 기준으로 입력 x를 z축 이동에 매핑합니다.
        // 오른쪽 입력은 Vector3.back 방향으로 이동합니다.
        Vector3 move = new Vector3(-input.x, 0f, 0f);

        cc.Move(move * moveSpeed * Time.deltaTime);
    }

    public void MoveVerticalVelocity()
    {
        // Jump와 ApplyGravity에서 누적한 수직 속도를 실제 위치에 반영합니다.
        cc.Move(velocity * Time.deltaTime);
    }

    public void MoveByVelocity(Vector3 moveVelocity)
    {
        // 대쉬처럼 외부에서 계산한 속도를 그대로 적용할 때 사용합니다.
        cc.Move(moveVelocity * Time.deltaTime);
    }

    public bool CanJump()
    {
        // 사용한 점프 횟수가 최대 점프 횟수보다 적으면 점프할 수 있습니다.
        return jumpCount < playerStatus.GetMaxJumpCount();
    }

    public void Jump()
    {
        if (!CanJump()) return;

        // 수직 속도를 점프력으로 덮어써 즉시 위로 튀어 오르게 합니다.
        velocity.y = playerStatus.GetJumpPower();
        jumpCount++;

        // 점프 직후에는 바닥에 있지 않은 상태로 취급해 착지 감지를 준비합니다.
        wasGrounded = false;
    }

    public void ApplyGravity()
    {
        bool isGrounded = cc.isGrounded;

        // 이전 프레임에는 공중이었고 지금 바닥에 닿았다면 착지한 순간입니다.
        if (!wasGrounded && isGrounded && velocity.y <= 0f)
        {
            jumpCount = 0;
            canAirDash = true;
            velocity.y = -2f;
        }

        // 바닥에 있을 때는 약한 음수 속도를 유지해 CharacterController가 바닥에 붙어 있게 합니다.
        if (isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f;
            canAirDash = true;
        }

        // 중력을 수직 속도에 누적합니다.
        velocity.y += gravity * Time.deltaTime;

        // 다음 프레임에서 착지 여부를 비교하기 위해 현재 바닥 상태를 저장합니다.
        wasGrounded = isGrounded;
    }

    public bool CanDash()
    {
        // 마지막 대쉬 이후 쿨타임이 지나지 않았다면 대쉬할 수 없습니다.
        if (Time.time < lastDashTime + dashCooldown) return false;

        // 지상에서는 쿨타임만 만족하면 대쉬할 수 있습니다.
        if (IsGrounded) return true;

        // 공중에서는 아직 공중 대쉬를 소비하지 않았을 때만 대쉬할 수 있습니다.
        return canAirDash;
    }

    public void ConsumeDash()
    {
        // 대쉬 사용 시점을 기록해 쿨타임 계산에 사용합니다.
        lastDashTime = Time.time;

        if (!IsGrounded)
        {
            // 공중에서 사용했다면 착지 전까지 추가 공중 대쉬를 막습니다.
            canAirDash = false;
        }
    }

    public void ResetVerticalVelocity(float value = 0f)
    {
        // 대쉬 시작, 넉백, 특수 이동처럼 기존 수직 속도를 지워야 할 때 사용합니다.
        velocity.y = value;
    }
}
