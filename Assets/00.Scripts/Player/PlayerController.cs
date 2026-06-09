using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerAttack))]
[RequireComponent(typeof(PlayerParry))]
public class PlayerController : MonoBehaviour
{
    // 현재 실행 중인 플레이어 상태입니다.
    private PlayerBaseState _currentState;

    // Input System에서 생성된 입력 액션 클래스입니다.
    private PlayerInputAction _input;

    // 충돌과 이동을 담당하는 Unity CharacterController입니다.
    private CharacterController _cc;

    // 실제 이동, 점프, 중력, 대쉬 계산을 담당하는 컴포넌트입니다.
    private PlayerMovement _moveMent;

    // 공격 생성과 공격 방향 계산을 담당하는 컴포넌트입니다.
    private PlayerAttack _attack;

    // 패링 범위 판정과 투사체 제거를 담당하는 컴포넌트입니다.
    private PlayerParry _parry;

    // 플레이어가 마지막으로 바라본 방향입니다. true면 오른쪽, false면 왼쪽으로 취급합니다.
    private bool _isFacingRight = true;

    // 상태 인스턴스들은 Awake에서 한 번 생성해 재사용합니다.
    public IdleState IdleState { get; private set; }
    public MoveState MoveState { get; private set; }
    public JumpState JumpState { get; private set; }
    public FallState FallState { get; private set; }
    public DashState DashState { get; private set; }
    public HitState HitState { get; private set; }
    public DeathState DeathState { get; private set; }

    // 현재 프레임의 이동 입력입니다.
    public Vector2 MoveInput { get; private set; }

    // 점프 버튼이 이번 프레임에 눌렸는지 여부입니다.
    public bool JumpTriggered { get; private set; }

    // 점프 버튼이 현재 눌린 상태인지 여부입니다.
    public bool IsJumping { get; private set; }

    // 공격 버튼이 이번 프레임에 눌렸는지 여부입니다.
    public bool AttackTriggered { get; private set; }

    // 대쉬 버튼이 이번 프레임에 눌렸는지 여부입니다.
    public bool DashTriggered { get; private set; }

    // 패링 버튼이 이번 프레임에 눌렸는지 여부입니다.
    public bool ParryTriggered { get; private set; }

    // 다른 상태와 컴포넌트에서 필요한 참조를 읽기 전용으로 제공합니다.
    public CharacterController Cc => _cc;
    public PlayerMovement Movement => _moveMent;
    public PlayerAttack Attack => _attack;
    public PlayerParry Parry => _parry;
    public bool IsFacingRight => _isFacingRight;

    // 현재 상태가 피해를 받을 수 있는지 Status 컴포넌트에서 확인할 때 사용합니다.
    public bool CanTakeDamage => _currentState == null || _currentState.CanTakeDamage;

    // 현재 상태에서 패링 판정을 사용할 수 있는지 PlayerParry에서 확인할 때 사용합니다.
    public bool CanParry => _currentState == null || _currentState.CanParry;

    private void Awake()
    {
        // 입력 액션과 필수 컴포넌트들을 초기화합니다.
        EnsurePlayerParry();

        _input = new PlayerInputAction();
        _cc = GetComponent<CharacterController>();
        _moveMent = GetComponent<PlayerMovement>();
        _attack = GetComponent<PlayerAttack>();
        _parry = GetComponent<PlayerParry>();

        // 상태 객체를 미리 만들어두고, 이후에는 TransitionTo로 상태만 교체합니다.
        IdleState = new IdleState(this);
        MoveState = new MoveState(this);
        JumpState = new JumpState(this);
        FallState = new FallState(this);
        DashState = new DashState(this);
        HitState = new HitState(this);
        DeathState = new DeathState(this);
    }

    private void OnEnable()
    {
        // 오브젝트가 활성화될 때 입력을 받을 수 있게 합니다.
        _input.Enable();
    }

    private void OnDisable()
    {
        // 비활성화 시 입력도 함께 끄면 불필요한 입력 처리와 이벤트 누수를 막을 수 있습니다.
        _input.Disable();
    }

    private void Start()
    {
        // 게임 시작 시 기본 상태는 정지 상태입니다.
        TransitionTo(IdleState);
    }

    private void Update()
    {
        // 매 프레임 입력을 먼저 읽고, 그 입력을 바탕으로 상태 로직을 실행합니다.
        PlayerInput();
        UpdateFacingDirection();

        if (_currentState != null && _currentState.CanParry)
        {
            HandleParryInput();
        }

        _currentState?.UpdateState();

        // 상태가 공격을 허용하는 경우에만 공격 입력을 처리합니다.
        // 예를 들어 DashState에서는 CanAttack이 false라 공격이 막힙니다.
        if (_currentState != null && _currentState.CanAttack)
        {
            HandleAttackInput();
        }
    }

    private void FixedUpdate()
    {
        _currentState?.FixedUpdateState();
    }

    public void TransitionTo(PlayerBaseState newState)
    {
        // 같은 상태로 다시 전환하려는 경우에는 중복 Enter/Exit 호출을 막습니다.
        if (_currentState == newState) return;

        // 기존 상태를 종료하고 새 상태에 진입합니다.
        _currentState?.ExitState();
        _currentState = newState;
        _currentState.EnterState();
    }

    public void EnterHitState(DamageInfo damageInfo)
    {
        // PlayerStatus에서 실제 데미지가 적용된 뒤 피격 상태로 진입할 때 사용합니다.
        HitState.SetHit(damageInfo);
        TransitionTo(HitState);
    }

    public void EnterDeathState(DeathInfo deathInfo)
    {
        // PlayerStatus에서 사망이 확정된 뒤 모든 조작을 잠그기 위해 사용합니다.
        DeathState.SetDeath(deathInfo);
        TransitionTo(DeathState);
    }

    public void ExitDeathStateAfterRevive()
    {
        // 부활 후 현재 위치의 지상 여부에 따라 자연스러운 기본 상태로 복귀합니다.
        if (Movement != null && !Movement.IsGrounded)
        {
            TransitionTo(FallState);
            return;
        }

        TransitionTo(IdleState);
    }

    private void PlayerInput()
    {
        // Input System 액션에서 현재 프레임 입력 값을 읽어 상태들이 사용할 수 있게 저장합니다.
        MoveInput = _input.Player.Move.ReadValue<Vector2>();
        JumpTriggered = _input.Player.Jump.WasPressedThisFrame();
        IsJumping = _input.Player.Jump.IsPressed();
        AttackTriggered = _input.Player.Attack.WasPressedThisFrame();
        DashTriggered = _input.Player.Dash.WasPressedThisFrame();
        ParryTriggered = _input.Player.Parry.WasPressedThisFrame();
    }

    private void UpdateFacingDirection()
    {
        // 대쉬 중에는 방향이 고정되어야 하므로 바라보는 방향을 갱신하지 않습니다.
        if (_currentState != null && !_currentState.CanUpdateFacingDirection)
            return;

        // x 입력을 기준으로 마지막 바라본 방향을 갱신합니다.
        if (MoveInput.x > 0f)
        {
            _isFacingRight = true;
        }
        else if (MoveInput.x < 0f)
        {
            _isFacingRight = false;
        }
    }

    private void HandleAttackInput()
    {
        // 이번 프레임에 공격 입력이 없으면 공격하지 않습니다.
        if (!AttackTriggered) return;

        // 공격 컴포넌트가 없는 예외 상황에서는 안전하게 종료합니다.
        if (_attack == null) return;

        // 공격 방향 계산에 이동 입력, 바라보는 방향, 지상 여부를 넘깁니다.
        _attack.Attack(MoveInput, _isFacingRight, _cc.isGrounded);
    }

    private void HandleParryInput()
    {
        // 패링 입력이 들어온 프레임에만 즉시 판정을 수행합니다.
        if (!ParryTriggered) return;

        if (_parry == null) return;

        _parry.TryParry();
    }

    private void EnsurePlayerParry()
    {
        // RequireComponent는 새로 붙일 때만 보장되므로, 기존 플레이어 오브젝트도 런타임에 보강합니다.
        if (GetComponent<PlayerParry>() != null) return;

        gameObject.AddComponent<PlayerParry>();
    }
}
