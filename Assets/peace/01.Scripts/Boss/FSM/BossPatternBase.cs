using System;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// [시스템 아키텍처: 추상 베이스 클래스 (Template Method Pattern)]
/// 보스의 공통 기능(물리, 애니메이션 렌더링, 이벤트 수신)을 제공하는 뼈대입니다.
/// 자식 클래스(구체적인 보스)는 이 뼈대 위에서 추상 메서드(BuildPatterns 등)만 구현하여 조립됩니다.
/// </summary>
public abstract class BossPatternBase : MonoBehaviour, IInitializable, IBossLogics, IBossAnimatorMoveHandler
{
    public int Priority => (int)InitOrder.Boss + 1;

    #region [1. 인스펙터 노출 변수 (기획 데이터)]
    [Header("Animator")]
    [SerializeField] protected Animator anim;

    [Header("VFX")]
    [SerializeField] protected GameObject telegraph;
    protected RingDrawer telegraphDrawer; // 빈번한 GetComponent 방지를 위한 캐싱 메모리

    [Header("Position")]
    [Tooltip("Boss spawn position")][SerializeField] protected Transform spawnPos;
    [Tooltip("Player position")][SerializeField] protected Transform playerPos;
    [Tooltip("Movable ground area")][SerializeField] protected BoxCollider ground;

    [Header("Hit Stop")]
    [Tooltip("Hit stop frames")][SerializeField] protected int HitStopFrame;
    [Tooltip("Camera shake intensity")][SerializeField] protected float cameraShakeIntensity;

    [Header("Idle")]
    [SerializeField] protected float idleDurationTime;
    [Tooltip("Move speed")][Min(0)][SerializeField] protected float move_idleSpeed;

    [Header("Enranged")]
    [SerializeField] protected float durationEnranged;
    [SerializeField] protected float enrangedAtkSpeed_Mul;

    [Header("Groggy")]
    [SerializeField] protected float groggyDuration;
    #endregion

    #region [2. 상태 및 프로퍼티 (메모리 읽기 전용 통로)]
    // 플레이어와의 상대적 X축 거리. playerPos가 null일 경우의 예외 처리(안전장치) 포함.
    public float Distance => playerPos == null ? 0f : playerPos.position.x - transform.position.x;

    // 외부(BossController 등)에서 보스의 현재 상태를 읽어갈 수 있도록 캡슐화된 프로퍼티들
    public bool StateDone { get; private set; }
    public bool IsParryed => isParryed;
    public bool IsEnranged => isEnranged;
    public bool IsPhysicsOverridden => IsPhysicsControlOverridden;
    public BossStatus bossStatus { get; private set; }
    #endregion

    #region [3. 내부 상태 캐싱 변수 (State Caching)]
    protected Transform bossSkin;
    protected Rigidbody rb;
    protected Node ultimateAttack;

    protected float curTime_Anim = 0f;

    // [최적화]: 애니메이터(C++)로 매 프레임 불필요한 데이터를 쏘지 않기 위한 '더티 플래그(Dirty Flag)' 캐싱 변수
    protected float cachedLocomotionSpeed = -999f;
    // 이동 애니메이션은 Num/방향/MoveForward 중 하나라도 바뀔 때만 Animator에 다시 주입한다.
    // RougeHood처럼 거리 유지 상태를 매 프레임 판정하는 보스가 Idle/Walk를 계속 처음부터 재생하는 문제를 막기 위한 캐시다.
    protected int cachedLocomotionAnim = -1;
    protected Facing cachedLocomotionFacing = Facing.Left;
    protected AnimatorStateInfo animState;

    protected AttackType attackType;
    protected string attackId = BossAttackIds.A;
    protected Facing curFacing = Facing.Left;

    // [메모리 최적화: 룩업 테이블 (Look-Up Table, LUT)]
    // if-else 분기문으로 각도를 계산하지 않고, Enum 값을 정수 인덱스로 변환하여 O(1)의 속도로 Y축 회전값을 즉시 가져옵니다.
    protected readonly float[] rotValue =
    {
        228, -228,               // Idle (0)
        -90, 90, 0, 0, -90, 90,  // Attack A, B, C (1,2,3)
        -90, 90, -90, 90, -90, 90, // Ultimate 1, 2, 3 (4,5,6)
        -90, 90,                 // Chase (7)
        228, -228,               // Parry (8)
        -90, 90,                 // Groggy (9)
        -90, 90                  // Walking (10)
    };

    protected float curTime_Idle = 0f;

    // 플래그 변수들 (상태 제어용 스위치)
    protected bool telegraphExcuted = false;
    protected bool isParryed = false;
    protected bool chaseDone = false;
    protected bool attackDone = false;
    protected bool isParryCanceled = false;

    protected int parryCount = 0;
    protected int comboStep = 0;
    protected bool isGroggyAnimDone = false;

    protected bool isEnranged = false;
    protected float curEnrangedTime = 0f;
    protected float curEnrangedAtkSpeed = 1f;

    protected float x, y, z;
    protected float groundXMin;
    protected float groundXMax;
    #endregion

    #region [4. 초기화 및 생명주기 (Lifecycle)]
    public virtual void Init()
    {
        bossStatus = ServiceLocator_Y.Get<BossStatus>();
        // 자식 오브젝트가 있으면 첫 번째 자식을 스킨으로, 없으면 자기 자신을 스킨으로 캐싱
        bossSkin = transform.childCount > 0 ? transform.GetChild(0).transform : transform;
        rb = GetComponent<Rigidbody>();

        if (telegraph != null)
        {
            telegraphDrawer = telegraph.GetComponent<RingDrawer>();
            telegraphDrawer?.Init();
        }

        InitMoveBounds();
        BuildPatterns(); // 자식 클래스에서 행동 트리를 조립하도록 유도
    }

    protected virtual void OnEnable() => EventBus<ParryKeyDown>.action += ParryKeyDown;
    protected virtual void OnDisable() => EventBus<ParryKeyDown>.action -= ParryKeyDown;
    #endregion

    #region [5. 템플릿 메서드 (자식 클래스 강제 구현부)]
    // 상속받는 클래스(예: 신데렐라)가 반드시 자기 자신만의 방식으로 구현해야 하는 빈 껍데기 함수들입니다.
    protected abstract void BuildPatterns();
    protected abstract AttackType SelectAttack();
    protected abstract Node GetAttackNode(AttackType selectedAttackType);
    protected virtual void ResetPatternState() { }
    protected virtual bool IsPhysicsControlOverridden => false;
    #endregion

    #region [6. 공용 로직 API (행동 트리용 유틸리티)]

    // Attack ID를 안전하게 문자열로 변환 (Data Bridge 통과)
    protected virtual string ResolveAttackId(AttackType selectedAttackType)
    {
        return BossAttackIds.FromAttackType(selectedAttackType);
    }

    // 공격 상태 컨텍스트 갱신
    protected void SetAttackContext(AttackType selectedAttackType)
    {
        attackType = selectedAttackType;
        attackId = ResolveAttackId(selectedAttackType);
    }

    protected void SetAttackContext(AttackType selectedAttackType, string selectedAttackId)
    {
        attackType = selectedAttackType;
        attackId = selectedAttackId;
    }

    // 빈 노드 생성 팩토리 메서드 (Null 대체용 객체 패턴)
    protected Node CreateStateDoneNode() { return new Leaf(() => SetStateDone(true)); }
    protected Node CreateEmptyPatternNode() { return new Leaf(() => SetStateDone(true)); }

    protected void PublishParryImpact()
    {
        EventBus<CameraShakeEvent>.Publish(new CameraShakeEvent(cameraShakeIntensity));
        // 패링 성공 피드백은 사전신호 불릿타임보다 우선되는 CombatFeel 연출로 요청한다.
        EventBus<HitStopEvent>.Publish(new HitStopEvent(
            HitStopFrame,
            TimeEffectSource.Parry,
            TimeEffectPriority.High,
            TimeEffectGroups.CombatFeel));
    }

    //사전신호 재생
    protected abstract NodeState PlayTelegraph(float baseTime, TelegraphType type);

    // 패링 성공 시 메모리 정리 및 글로벌 이벤트 발송
    protected void Parryed()
    {
        telegraphExcuted = false;
        isParryed = false;
        isParryCanceled = true;
        EventBus<CanParryEvent>.Publish(new CanParryEvent(false));
        EventBus<ColliderToggleEvent>.Publish(new ColliderToggleEvent(attackId, false));
    }

    private void ParryKeyDown(ParryKeyDown data) { isParryed = true; }

    // 물리 이동 변수 세팅 (실행은 ExcuteMove에서 일괄 처리)
    protected void Move(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }

    public void ExcuteMove()
    {
        if (rb == null) return;
        rb.linearVelocity = new Vector3(x, y, z);
    }

    // 방향 업데이트 로직
    public void UpdateFacing()
    {
        // 1. 플레이어가 내 왼쪽에 있으면 Left, 아니면 Right
        Facing newFacing = (Distance < 0f) ? Facing.Left : Facing.Right;

        // 2. 이미 같은 방향을 보고 있다면 연산 중단 (Bypass 최적화)
        if (curFacing == newFacing) return;

        // 3. 방향이 바뀌었을 때만 상태를 갱신하고 콜라이더를 뒤집도록 이벤트 발송
        curFacing = newFacing;
        EventBus<BossFacingChangeEvent>.Publish(new BossFacingChangeEvent(curFacing));
    }

    /// <summary>
    /// 논리적 시선(Facing)과 시각적 애니메이션(Animator), 모델링 회전(Rotation)을 동기화합니다.
    /// </summary>
    public void SyncFacingWithAnim(int targetAnim)
    {
        if (anim == null || bossSkin == null) return;

        // 애니메이터에 현재 시선 전달 (블렌드 트리 역재생 등을 위함)
        bool shouldMirror = (curFacing == Facing.Right);
        anim.SetBool("isMirrored", shouldMirror);
        anim.SetInteger("Num", targetAnim);

        // 룩업 테이블(rotValue)을 이용한 O(1) 회전값 계산
        // (타겟 애니메이션 번호 * 2) + (방향에 따른 보정치 0 또는 1)
        int angleIndex = (curFacing == Facing.Left) ? (targetAnim * 2) : (targetAnim * 2 + 1);
        if (angleIndex >= 0 && angleIndex < rotValue.Length)
            bossSkin.rotation = Quaternion.Euler(0f, rotValue[angleIndex], 0f);
    }

    /// <summary>
    /// [드라이 런: 상태 전이 오염 방지망]
    /// 애니메이터가 목표 상태로 완전히 진입했는지 검증하여 애니메이션 전환 중 꼬임을 방지합니다.
    /// </summary>
    public bool IsAnimationReady(int targetAnim)
    {
        if (anim == null) return true;

        int curAnim = anim.GetInteger("Num");
        bool curMirror = anim.GetBool("isMirrored");
        bool targetMirror = (curFacing == Facing.Right);

        // 1. 목표 애니메이션과 현재 애니메이션이 다르다면?
        if (curAnim != targetAnim)
        {
            curTime_Anim = 0f; // 애니메이션 타이머 초기화
            SyncFacingWithAnim(targetAnim); // 방향 동기화 후
            return false; // 아직 준비 안 됨을 반환
        }

        // 2. 애니메이션은 같은데 방향이 다르다면?
        if (curMirror != targetMirror)
        {
            SyncFacingWithAnim(targetAnim);
            return false;
        }

        // 3. 엔진 내부에서 부드럽게 전환(Transition) 중이라면?
        return !anim.IsInTransition(0); // 전환이 완전히 끝났을 때만 true 반환
    }

    /// <summary>
    /// [시공간 제어 로직] 지정된 현실 시간(targetSeconds) 안에 애니메이션 재생을 강제로 끝마치도록 속도를 수학적으로 통제합니다.
    /// </summary>
    public NodeState PlayAnim_Speed(int num, float targetSeconds)
    {
        if (anim == null) return NodeState.Success;
        if (!IsAnimationReady(num)) return NodeState.Running;

        animState = anim.GetCurrentAnimatorStateInfo(0);

        // 1. 기본 배율 계산: 애니메이션 원본 길이(length) / 내가 끝내고 싶은 시간(target)
        // ex) 2초짜리 애니메이션을 1초만에 끝내려면 배율은 2.0이 됨. (targetSeconds가 0이면 원본 속도 1 유지)
        float baseSpeed = targetSeconds == 0f ? 1f : animState.length / targetSeconds;

        // 2. 격노 상태(Enranged) 배율 적용: 특정 공격 모션일 경우 배율을 추가 곱셈 연산
        anim.speed = IsEnrangedAttackAnimation(num) ? baseSpeed * curEnrangedAtkSpeed : baseSpeed;

        // 3. 진행도(normalizedTime)가 95% 미만이면 계속 실행 (Running)
        if (animState.normalizedTime < 0.95f) return NodeState.Running;

        // 4. 완료 시 애니메이터 오염을 막기 위해 속도를 무조건 1.0 정상으로 복구
        anim.speed = 1.0f;
        return NodeState.Success;
    }

    // 단순히 시간만 카운팅하며 애니메이션을 재생하는 메서드
    public NodeState PlayAnim_Time(int num, float targetSeconds)
    {
        if (anim == null) return NodeState.Success;
        if (!IsAnimationReady(num)) return NodeState.Running;

        curTime_Anim += Time.deltaTime;
        animState = anim.GetCurrentAnimatorStateInfo(0);

        if (curTime_Anim < targetSeconds) return NodeState.Running;

        anim.speed = 1.0f;
        return NodeState.Success;
    }

    protected virtual bool IsEnrangedAttackAnimation(int animNum)
    {
        return animNum == (int)Animation.AttackA ||
            animNum == (int)Animation.AttackB ||
            animNum == (int)Animation.AttackC;
    }

    // 오브젝트 풀링 개념: 이펙트를 새로 Instantiate하지 않고 위치만 옮겨 재사용(GC 발생 0%)
    public void PlayEffect(ParticleSystem excuteEffect, float x, float y, float z)
    {
        if (excuteEffect == null) return;
        Vector3 effectPos = new Vector3(x, y, z);
        excuteEffect.transform.position = effectPos;
        excuteEffect.Play();
    }

    public bool IsAttacking()
    {
        if (anim == null) return false;

        int currentAnim = anim.GetInteger("Num");
        return currentAnim == (int)Animation.AttackA ||
            currentAnim == (int)Animation.AttackB ||
            currentAnim == (int)Animation.AttackC ||
            currentAnim == (int)Animation.Ultimate1 ||
            currentAnim == (int)Animation.Ultimate2 ||
            currentAnim == (int)Animation.Ultimate3;
    }

    public NodeState SetStateDone(bool set)
    {
        if (StateDone != set) StateDone = set;
        return NodeState.Success;
    }

    public bool GetStateDone() { return StateDone; }
    public void Spawn() { if (spawnPos != null) transform.position = spawnPos.position; }

    public virtual void InitCurTime_Idle()
    {
        curTime_Idle = 0f;
        cachedLocomotionSpeed = -999f;
        cachedLocomotionAnim = -1;
        cachedLocomotionFacing = curFacing;
    }

    /// <summary>
    /// [공간 제어]: 플레이어의 주변을 기준으로 보스가 순찰(Idle)할 무작위 좌표를 수학적으로 1회 계산합니다.
    /// </summary>
    public virtual void SetRandomPos()
    {
        if (playerPos == null)
        {
            return;
        }

        // 1. 최소 유지거리(move_idlePos)에 무작위 범위(move_idleRange)를 더해 절대적인 거리를 구함
        // 기본 구현은 목적지를 계산하지 않는다.
        // Cinderella_Patterns가 아래 주석의 랜덤 목적지 계산을 override해서 실제로 수행한다.

        // 2. 플레이어가 내 왼쪽에 있으면 플레이어의 오른쪽(+)으로 맴돌고, 반대면 반대로 맴돔
        // if (Math.Sign(Distance) <= 0) RandomPos = new Vector3(playerPos.position.x + element, 0, 0);
        // else RandomPos = new Vector3(playerPos.position.x - element, 0, 0);

        // 3. 계산된 좌표가 무대를 벗어나지 않도록 Ground Collider의 Min/Max로 클램핑(강제 범위 고정)
        // if (ground != null)
        //     RandomPos.x = Mathf.Clamp(RandomPos.x, groundXMin, groundXMax);
    }

    /// <summary>
    /// [최적화 아키텍처: 더티 플래그(Dirty Flag) 패턴]
    /// 매 틱마다 C#이 C++ 애니메이터를 호출하는 마샬링 병목을 제거합니다. 값이 변했을 때만 엔진에 하달합니다.
    /// </summary>
    protected void SyncLocomotionAnim(float moveDirX)
    {
        int targetAnim;
        float targetSpeed; //애니메이션 역재생 조절 변수

        if (moveDirX == 0f)
        {
            targetAnim = (int)Animation.Idle;
            targetSpeed = 0f;
        }
        else
        {
            targetAnim = (int)Animation.Walking;
            float facingDir = (curFacing == Facing.Right) ? 1f : -1f; // 시선 벡터
            targetSpeed = moveDirX * facingDir; // 내적 응용 (방향 * 시선 = 전진/후진 판별)
        }

        // 캐시된 값과 목표값이 동일하면(상태 오염 없음) 즉시 리턴 (Bypass)
        // Num/방향/MoveForward를 모두 비교해야 Walk <-> Idle뿐 아니라 좌우 반전만 바뀌는 경우도 정확히 갱신된다.
        if (cachedLocomotionAnim == targetAnim &&
            cachedLocomotionFacing == curFacing &&
            Mathf.Abs(cachedLocomotionSpeed - targetSpeed) < 0.01f)
            return;

        // 값이 다를 때만 1회성(Edge Trigger)으로 렌더링 호출
        // PlayAnim_Time은 대기 시간 계산까지 포함하므로, 이동 루프에서는 애니메이션 전환만 담당하는 SyncFacingWithAnim을 직접 호출한다.
        // 이렇게 해야 매 프레임 Idle/Walk 애니메이션이 0프레임으로 되감기는 현상을 피할 수 있다.
        SyncFacingWithAnim(targetAnim);
        if (anim != null) anim.SetFloat("MoveForward", targetSpeed);
        // 메모리 동기화
        cachedLocomotionAnim = targetAnim;
        cachedLocomotionFacing = curFacing;
        cachedLocomotionSpeed = targetSpeed;
    }

    // 플레이어가 보스 기준 어느 방향에 있는지 1D 방향값으로 변환한다.
    // Distance > 0이면 플레이어는 오른쪽, Distance < 0이면 왼쪽이다.
    protected float GetDirectionToPlayer()
    {
        if (Mathf.Approximately(Distance, 0f))
            return curFacing == Facing.Right ? 1f : -1f;

        return Mathf.Sign(Distance);
    }

    // 맵 끝에서 바깥쪽으로 계속 이동하려는 입력을 0으로 막는다.
    // RougeHood의 거리 유지 AI가 플레이어 반대편으로 물러나다가 ground bounds 밖으로 나가는 상황을 방지한다.
    protected float BlockMoveOutsideGround(float moveDirX, float edgePadding)
    {
        if (ground == null || Mathf.Approximately(moveDirX, 0f))
            return moveDirX;

        float minX = groundXMin + edgePadding;
        float maxX = groundXMax - edgePadding;
        float curX = transform.position.x;

        if (moveDirX < 0f && curX <= minX) return 0f;
        if (moveDirX > 0f && curX >= maxX) return 0f;

        return moveDirX;
    }

    /// <summary>
    /// [서브 스테이트 머신]: 타겟 도착 전(Walking)과 후(Idling)를 명확히 분리하여 지터링(Jittering)을 막는 로직.
    /// </summary>
    public abstract void IdleMove();

    public AttackType GetAttackType() { return attackType; }
    public string GetAttackId() { return attackId; }

    public Node GetAttackBT()
    {
        SetAttackContext(SelectAttack());
        return GetAttackNode(attackType) ?? CreateEmptyPatternNode();
    }

    protected NodeState Chase(Vector3 pos, float speed, int animNum)
    {
        UpdateFacing();
        if (!IsAnimationReady(animNum))
        {
            Move(0, 0, 0);
            return NodeState.Running;
        }

        if (Math.Abs(Distance) > pos.x)
        {
            Move(Math.Sign(Distance) * speed, 0, 0);
            return NodeState.Running;
        }

        Move(0, 0, 0);
        chaseDone = true;
        return NodeState.Success;
    }

    public virtual void LogicInit()
    {
        chaseDone = false;
        telegraphExcuted = false;
        attackDone = false;
        isParryCanceled = false;
        parryCount = 0;
        comboStep = 0;
        ultimateAttack?.Reset();
        isGroggyAnimDone = false;
        ResetPatternState();
    }

    public Node GetUltimateBT() { return ultimateAttack ?? CreateEmptyPatternNode(); }
    public virtual bool CanTransitionToGroggy() { return parryCount >= 3; }

    public NodeState PlayAnimGroggy_Time(int num)
    {
        if (!isGroggyAnimDone)
        {
            if (PlayAnim_Time(num, groggyDuration) == NodeState.Success)
            {
                isGroggyAnimDone = true;
                bossStatus?.SetGroggyDamageMultiplierActive(false);
                return NodeState.Running;
            }
        }
        else if (PlayAnim_Time((int)Animation.Idle, 1) == NodeState.Success)
        {
            isEnranged = true;
            curEnrangedAtkSpeed = enrangedAtkSpeed_Mul;
            return NodeState.Success;
        }

        return NodeState.Running;
    }

    protected float GetAdjustedTelegraphTime(float baseTime)
    {
        // 0으로 나누는 에러(DivideByZero) 방지용 안전장치(Fail-safe)
        return curEnrangedAtkSpeed == 0f ? baseTime : baseTime / curEnrangedAtkSpeed;
    }

    public void EnrangedTimer()
    {
        curEnrangedTime += Time.deltaTime;
        if (curEnrangedTime < durationEnranged) return;

        isEnranged = false;
        curEnrangedTime = 0f;
        curEnrangedAtkSpeed = 1f;
    }

    public virtual void OnAnimatorMoveCallback() { }

    private void InitMoveBounds()
    {
        if (ground != null)
        {
            groundXMin = ground.bounds.min.x;
            groundXMax = ground.bounds.max.x;
        }
        else
        {
            // 그라운드가 미할당 상태여도 게임이 뻗지 않도록 무한대 값 부여 (Fallback)
            groundXMin = float.NegativeInfinity;
            groundXMax = float.PositiveInfinity;
        }

        // 기획 데이터 기입 실수 방지 (최소 사거리 보정)
        // 거리 유지 방식은 보스마다 다르므로, 최소/최대 거리 보정은 각 보스 구현에서 처리한다.
    }
    #endregion
}
