using System;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// [시스템 아키텍처: 추상 베이스 클래스 (Template Method Pattern)]
/// 모든 보스(신데렐라, 루주후드 등)가 공통으로 사용하는 로우 레벨(Low-level) 기능(물리, 애니메이션 마샬링, 상태 이벤트 수신)을 제공하는 뼈대입니다.
/// 자식 클래스는 이 클래스를 상속받아 구체적인 행동 트리(BT) 조립과 기획 데이터 주입에만 집중(관심사의 분리, SRP)할 수 있습니다.
/// </summary>
public abstract class BossPatternBase : MonoBehaviour, IInitializable, IBossLogics, IBossAnimatorMoveHandler
{
    // 시스템 초기화 우선순위 (Player -> Mob -> Boss 순으로 초기화 보장)
    public int Priority => (int)InitOrder.Boss + 1;

    #region [1. 인스펙터 노출 변수 (기획 데이터)]
    [Header("Animator")]
    [SerializeField] protected Animator anim;

    [Header("VFX")]
    [SerializeField] protected GameObject telegraph;
    // 런타임 중 빈번한 GetComponent 연산을 피하기 위해 메모리에 올려두는 사전신호 UI 컨트롤러 캐싱
    protected RingDrawer telegraphDrawer;

    [Header("Position")]
    [Tooltip("월드 Z축 고정 (횡스크롤 평면 유지용)")][SerializeField] protected Transform worldZPos;
    [Tooltip("보스 최초 스폰 지점 좌표")][SerializeField] protected Transform spawnPos;
    [Tooltip("플레이어의 현재 위치 추적용 트랜스폼")][SerializeField] protected Transform playerPos;
    [Tooltip("보스가 맵 밖으로 나가지 못하게 제한하는 이동 가능 구역(경계) 콜라이더")][SerializeField] protected BoxCollider ground;

    [Header("Hit Stop")]
    [Tooltip("플레이어가 패링 성공 시 화면이 멈추는 프레임 수 (타격감 연출)")][SerializeField] protected int HitStopFrame;
    [Tooltip("패링 성공 시 발생하는 카메라 진동의 강도")][SerializeField] protected float cameraShakeIntensity;

    [Header("Idle")]
    [Tooltip("순찰(배회) 후 대기하는 시간")][SerializeField] protected float idleDurationTime;
    [Tooltip("순찰 시 이동 속도")][Min(0)][SerializeField] protected float move_idleSpeed;

    [Header("Enranged (격노)")]
    [Tooltip("그로기 이후 격노 상태가 유지되는 총 시간")][SerializeField] protected float durationEnranged;
    [Tooltip("격노 상태 시 적용될 공격 속도 배율 (예: 1.5면 1.5배 빨라짐)")][SerializeField] protected float enrangedAtkSpeed_Mul;

    [Header("Groggy")]
    [Tooltip("패링 3회 누적 시 보스가 무력화되는 시간")][SerializeField] protected float groggyDuration;
    [Tooltip("그로기 애니메이션 동안 반복할 SFX")][SerializeField] protected AudioClip groggySfx;
    [Tooltip("그로기 반복 SFX의 재생 볼륨")][SerializeField, Range(0f, 1f)] protected float groggySfxVolume = 1f;
    [Tooltip("그로기 SFX 종료 시 페이드 아웃 시간")][SerializeField, Min(0f)] protected float groggySfxFadeOutDuration = 0.1f;
    #endregion

    #region [2. 상태 및 프로퍼티 (외부 접근용 읽기 전용 통로)]
    /// <summary>
    /// [1D 공간 계산]: 플레이어 위치 - 보스 위치. 
    /// 값이 양수(+)면 플레이어가 보스보다 오른쪽에, 음수(-)면 왼쪽에 있음을 의미합니다.
    /// playerPos가 파괴되거나 null일 경우 오류(NRE)를 막기 위해 0f를 반환하는 방어막(Fail-safe)이 적용되어 있습니다.
    /// </summary>
    public float Distance => playerPos == null ? 0f : playerPos.position.x - transform.position.x;

    // 외부(BossController, BT 노드 등)에서 보스의 현재 상태를 확인하기 위한 프로퍼티 캡슐화
    public bool StateDone { get; private set; }             // 현재 진행 중인 행동(공격 등)이 완전히 끝났는가?
    public bool IsParryed => isParryed;                     // 이번 프레임에 플레이어에게 패링을 당했는가?
    public bool IsEnranged => isEnranged;                   // 현재 공속이 빨라지는 격노 상태인가?
    public bool IsPhysicsOverridden => IsPhysicsControlOverridden; // 현재 루트 모션(점프 등)이 물리 엔진을 통제 중인가?
    public BossStatus bossStatus { get; private set; }      // 보스의 체력/스탯 데이터 컨테이너
    #endregion

    #region [3. 내부 상태 캐싱 변수 (State Caching)]
    protected Transform bossSkin; // 보스 3D 모델링의 실제 회전(좌/우 보기)을 담당하는 트랜스폼
    protected Rigidbody rb;       // 물리 이동 연산용 컴포넌트
    protected Node ultimateAttack;// 조립이 완료된 궁극기 행동 트리 노드

    protected float curTime_Anim = 0f; // 애니메이션 재생 시간 추적 타이머

    // [최적화 아키텍처: 더티 플래그(Dirty Flag) 패턴용 변수들]
    // C# 코드에서 유니티 C++ 네이티브 단(Animator)으로 매 프레임 데이터를 보내는 마샬링(Marshalling) 병목을 막기 위한 캐싱 변수입니다.
    protected float cachedLocomotionSpeed = -999f;
    protected int cachedLocomotionAnim = -1;
    protected Facing cachedLocomotionFacing = Facing.Left;
    protected AnimatorStateInfo animState; // 현재 재생 중인 애니메이션의 진행도(normalizedTime)와 길이를 읽어오는 캐시

    protected AttackType attackType;            // 현재 진행 중인 공격의 열거형 타입 (A, B, C 등)
    protected string attackId = BossAttackIds.A;// 콜라이더 시스템(AttackColliders_Y)과 통신하기 위한 문자열 ID
    protected Facing curFacing = Facing.Left;   // 보스가 현재 논리적으로 바라보고 있는 방향

    // [메모리 최적화: 룩업 테이블 (Look-Up Table, LUT)]
    // 보스의 상태(Animation Enum)와 바라보는 방향(Facing)에 따라 모델링의 Y축 회전값을 결정합니다.
    // 매 프레임 if-else나 switch 문을 쓰지 않고 O(1)의 배열 인덱스 접근 속도로 값을 즉시 가져옵니다.
    protected readonly float[] rotValue =
    {
        -90, 90,               // [0, 1] Idle 상태일 때 좌/우 회전각
        -90, 90, -90, 90, -90, 90,  // [2~7] Attack A, B, C 상태일 때 좌/우 회전각
        -90, 90, -90, 90, -90, 90, // [8~13] Ultimate 1, 2, 3 상태일 때 좌/우 회전각
        -90, 90,                 // [14, 15] Chase(추격) 상태일 때 좌/우 회전각
        228, -228,               // [16, 17] Parry(피격 경직) 상태일 때 좌/우 회전각
        -90, 90,                 // [18, 19] Groggy(무력화) 상태일 때 좌/우 회전각
        -90, 90                  // [20, 21] Walking(이동) 상태일 때 좌/우 회전각
    };

    protected float curTime_Idle = 0f; // 배회(Idle) 상태 머무른 시간 카운터

    // [논리 상태 플래그 변수들]
    protected bool telegraphExcuted = false; // 사전 신호(Telegraph)가 1회 실행되었는가?
    protected bool isParryed = false;        // 플레이어에 의해 패링 당했는가?
    protected bool chaseDone = false;        // 타겟 위치까지 추격을 완료했는가?
    protected bool attackDone = false;       // 공격 타격 모션이 완료되었는가?
    protected bool isParryCanceled = false;  // 공격이 패링에 의해 강제 취소되었는가?

    protected int parryCount = 0;            // 누적된 패링 횟수 (3회 도달 시 그로기)
    protected int comboStep = 0;             // 다단 히트 공격(예: 궁극기 3연타)의 현재 진행 스텝
    protected bool isGroggyAnimDone = false; // 그로기 쓰러짐 애니메이션이 완료되었는가?
    protected bool isGroggySfxPlaying = false; // 그로기 루프 SFX가 이미 시작되었는가?

    protected bool isEnranged = false;       // 현재 격노 상태 플래그
    protected float curEnrangedTime = 0f;    // 격노 지속 타이머
    protected float curEnrangedAtkSpeed = 1f;// 현재 적용 중인 공격 속도 배율 (기본 1.0)

    // [물리 이동 변수]
    protected float x, y, z;      // FixedUpdate에서 적용할 목표 이동 속도 벡터 성분
    protected float groundXMin;   // 보스가 이동 가능한 좌측 한계(Limit) 좌표
    protected float groundXMax;   // 보스가 이동 가능한 우측 한계(Limit) 좌표
    #endregion

    #region [4. 초기화 및 생명주기 (Lifecycle)]
    public virtual void Init()
    {
        // ServiceLocator를 통한 전역 상태 컨테이너 주입
        bossStatus = ServiceLocator.Get<BossStatus>();

        // 자식 오브젝트가 있으면 첫 번째 자식을 모델링(스킨)으로, 없으면 자기 자신을 스킨으로 캐싱
        bossSkin = transform.childCount > 0 ? transform.GetChild(0).transform : transform;
        rb = GetComponent<Rigidbody>();

        // 링 UI 컨트롤러(사전신호) 초기화
        if (telegraph != null)
        {
            telegraphDrawer = telegraph.GetComponent<RingDrawer>();
            telegraphDrawer?.Init();
        }

        InitMoveBounds(); // 맵 이동 한계선 설정
        BuildPatterns();  // [템플릿 메서드 호출]: 자식 클래스에서 행동 트리를 조립하도록 유도
    }

    // 전역 이벤트(EventBus) 구독 연결 (패링 입력 수신)
    protected virtual void OnEnable() => EventBus<ParryKeyDown>.action += ParryKeyDown;
    protected virtual void OnDisable()
    {
        EventBus<ParryKeyDown>.action -= ParryKeyDown;
        telegraphDrawer?.StopSignal(); // 비활성화 뒤에도 남을 수 있는 사전신호와 연출 코루틴을 정리
        StopGroggySfx(); // 상태 Exit 없이 보스가 비활성화되어도 그로기 반복음이 남지 않게 정리
        // 비활성화되는 보스가 열어 둔 패링 창만 닫도록 현재 attackId를 함께 보낸다.
        EventBus<CanParryEvent>.Publish(
            new CanParryEvent(attackId, false)); // PlayerParry의 보스 패링 창을 강제로 닫음
    }
    #endregion

    #region [5. 템플릿 메서드 (자식 클래스 강제 구현부)]
    // 이 클래스를 상속받는 구체적 보스(신데렐라, 루주후드)가 반드시 자기 자신만의 논리로 구현해야 하는 계약(Contract)입니다.
    protected abstract void BuildPatterns(); // 행동 트리(BT) 메모리 조립 지시
    protected abstract AttackType SelectAttack(); // 어떤 공격을 할지(거리 비례 or 랜덤) AI 판단 지시
    protected abstract Node GetAttackNode(AttackType selectedAttackType); // 결정된 공격 타입의 BT 노드 반환
    protected virtual void ResetPatternState() { } // 보스 고유의 오염된 상태(점프 플래그 등) 초기화 함수
    protected virtual bool IsPhysicsControlOverridden => false; // 강제 루트 모션 제어 여부 반환
    #endregion

    #region [6. 공용 로직 API (행동 트리용 유틸리티)]

    /// <summary>
    /// Enum 타입의 AttackType을 파싱 최적화 클래스(BossAttackIds)를 거쳐 안전한 문자열 ID로 변환합니다.
    /// </summary>
    protected virtual string ResolveAttackId(AttackType selectedAttackType)
    {
        return BossAttackIds.FromAttackType(selectedAttackType);
    }

    // 공격 상태 컨텍스트 갱신 (콜라이더 시스템 통신 준비)
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

    // 행동 트리 빈 노드 생성 팩토리 (Null 대체용 Object 패턴)
    protected Node CreateStateDoneNode() { return new Leaf(() => SetStateDone(true)); }
    protected Node CreateEmptyPatternNode() { return new Leaf(() => SetStateDone(true)); }

    /// <summary>
    /// 플레이어의 패링 성공 시, 카메라 진동과 엔진 역경직(HitStop)을 발생시켜 강렬한 타격감을 연출합니다.
    /// </summary>
    protected void PublishParryImpact()
    {
        EventBus<CameraShakeEvent>.Publish(new CameraShakeEvent(cameraShakeIntensity));

        // 패링 성공 피드백은 UI의 불릿타임 연출보다 높은 우선순위(High)를 가집니다.
        EventBus<HitStopEvent>.Publish(new HitStopEvent(HitStopFrame));
    }

    // 사전신호의 종류별 시작 처리를 자식 클래스에 위임한다.
    // 시간 진행은 각 신호가 사용하는 Animator 커브 또는 자체 구현이 담당하며, 이 API는 시간 값을 받지 않는다.
    protected abstract NodeState PlayTelegraph(TelegraphType type);

    /// <summary>
    /// 패링 성공 후 공격 콜라이더, 보스 패링 창, 사전신호를 닫고 공격 취소 상태를 기록한다.
    /// </summary>
    protected void Parryed()
    {
        telegraphDrawer?.StopSignal();
        telegraphExcuted = false;
        isParryed = false;
        isParryCanceled = true; // 공격 캔슬 플래그 발동
        // 패링 성공으로 공격이 취소됐으므로 이 공격의 패링 시간과 콜라이더를 모두 닫는다.
        EventBus<CanParryEvent>.Publish(
            new CanParryEvent(attackId, false));
        EventBus<ColliderToggleEvent>.Publish(new ColliderToggleEvent(attackId, false));
    }

    // EventBus 리시버: 플레이어가 패링 버튼을 눌렀음을 인지
    private void ParryKeyDown(ParryKeyDown data) { isParryed = true; }

    /// <summary>
    /// 물리적 이동의 방향과 속도를 메모리에 설정합니다. 실제 물리 반영은 FixedUpdate 내의 ExcuteMove에서 일괄 처리됩니다.
    /// </summary>
    protected void Move(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }

    public void ExcuteMove()
    {
        if (rb == null) return;
        rb.linearVelocity = new Vector3(x, y, z);
    }

    /// <summary>
    /// 보스의 논리적 시선(좌/우)을 플레이어 위치에 기반하여 동적으로 갱신합니다.
    /// </summary>
    public void UpdateFacing()
    {
        // 1. 플레이어가 내 왼쪽에 있으면 Left, 아니면 Right
        Facing newFacing = (Distance < 0f) ? Facing.Left : Facing.Right;

        // 2. 이미 같은 방향을 보고 있다면 불필요한 이벤트 발송과 연산을 생략합니다. (Bypass 최적화)
        if (curFacing == newFacing) return;

        // 3. 방향이 바뀌었을 때만 상태를 갱신하고 콜라이더를 뒤집도록 전역 이벤트 발송
        curFacing = newFacing;
        EventBus<BossFacingChangeEvent>.Publish(new BossFacingChangeEvent(curFacing));
    }

    /// <summary>
    /// 논리적 시선(Facing)과 시각적 애니메이션(Animator 블렌딩), 3D 모델링 회전(Rotation)을 일치시킵니다.
    /// </summary>
    public void SyncFacingWithAnim(int targetAnim)
    {
        if (anim == null || bossSkin == null) return;

        // 애니메이터 파라미터 업데이트 (블렌드 트리의 뒤로 걷기 역재생 등을 위함)
        bool shouldMirror = (curFacing == Facing.Right);
        anim.SetBool("isMirrored", shouldMirror);
        anim.SetInteger("Num", targetAnim);

        // [O(1) 룩업 연산]: 타겟 애니메이션 번호에 2를 곱하고, 방향(좌 0, 우 1)을 더해 회전값 배열 인덱스를 도출합니다.
        int angleIndex = (curFacing == Facing.Left) ? (targetAnim * 2) : (targetAnim * 2 + 1);
        if (angleIndex >= 0 && angleIndex < rotValue.Length)
            bossSkin.rotation = Quaternion.Euler(0f, rotValue[angleIndex], 0f);
    }

    /// <summary>
    /// [상태 전이 오염 방지망]: 애니메이터가 목표 상태로 완전히 진입했는지 검증하여 애니메이션 전환(Transition) 중 발생하는 꼬임을 방지합니다.
    /// </summary>
    public bool IsAnimationReady(int targetAnim)
    {
        if (anim == null) return true;

        int curAnim = anim.GetInteger("Num");
        bool curMirror = anim.GetBool("isMirrored");
        bool targetMirror = (curFacing == Facing.Right);

        // 1. 목표 애니메이션과 현재 엔진이 재생 중인 애니메이션이 다르다면?
        if (curAnim != targetAnim)
        {
            curTime_Anim = 0f; // 애니메이션 재생 타이머 초기화
            SyncFacingWithAnim(targetAnim); // 방향 재동기화 명령 하달
            return false; // 아직 준비 안 됨을 반환
        }

        // 2. 애니메이션은 같지만 방향(좌우 반전)이 어긋났다면?
        if (curMirror != targetMirror)
        {
            SyncFacingWithAnim(targetAnim);
            return false;
        }

        // 3. 엔진 내부에서 두 애니메이션이 부드럽게 섞이는(Transition) 중이라면?
        return !anim.IsInTransition(0); // 섞이는 과정이 완전히 끝났을 때만 true 반환하여 로직 실행 개시
    }

    /// <summary>
    /// [시공간 제어 로직]: 지정된 현실 시간(targetSeconds) 안에 애니메이션 재생을 강제로 끝마치도록 속도(Speed)를 수학적으로 통제합니다.
    /// </summary>
    public NodeState PlayAnim_Speed(int num, float targetSeconds)
    {
        if (anim == null) return NodeState.Success;
        if (!IsAnimationReady(num)) return NodeState.Running; // 렌더링 준비될 때까지 대기

        animState = anim.GetCurrentAnimatorStateInfo(0);

        // 1. 기본 배율 계산: 애니메이션 원본 길이(length) / 내가 끝내고 싶은 시간(targetSeconds)
        // ex) 2초짜리 원본 애니메이션을 1초만에 끝내려면 배율은 2.0이 됨. (targetSeconds가 0이면 원본 속도 1.0 유지)
        float baseSpeed = targetSeconds == 0f ? 1f : animState.length / targetSeconds;

        // 2. 격노 상태(Enranged) 배율 적용: 특정 공격 모션일 경우 미리 정해진 배율(curEnrangedAtkSpeed)을 추가 곱셈 연산합니다.
        anim.speed = IsEnrangedAttackAnimation(num) ? baseSpeed * curEnrangedAtkSpeed : baseSpeed;

        // 3. 진행도(normalizedTime)가 95% 미만이면 액션이 덜 끝났으므로 계속 실행 (Running) 반환
        if (animState.normalizedTime < 0.95f) return NodeState.Running;

        // 4. 완료 시, 변경되었던 엔진 애니메이터의 재생 속도 오염을 막기 위해 무조건 1.0(정상)으로 복구합니다.
        anim.speed = 1.0f;
        return NodeState.Success;
    }

    /// <summary>
    /// 애니메이션의 자체 재생 속도(Speed)는 건드리지 않고, 지정된 시간(targetSeconds)만큼 대기하며 애니메이션을 지속 재생합니다.
    /// </summary>
    public NodeState PlayAnim_Time(int num, float targetSeconds)
    {
        if (anim == null) return NodeState.Success;
        if (!IsAnimationReady(num)) return NodeState.Running;

        curTime_Anim += Time.deltaTime; // C# 로직 상의 자체 타이머 증가
        animState = anim.GetCurrentAnimatorStateInfo(0);

        // 지정된 대기 시간을 채우지 못했다면 루프 지속
        if (curTime_Anim < targetSeconds) return NodeState.Running;

        anim.speed = 1.0f;
        return NodeState.Success;
    }

    /// <summary>
    /// 현재 재생하려는 애니메이션이 '격노 배율(공격 속도 증가)'의 영향을 받는 공격 모션인지 필터링합니다.
    /// </summary>
    protected virtual bool IsEnrangedAttackAnimation(int animNum)
    {
        return animNum == (int)Animation.AttackA ||
            animNum == (int)Animation.AttackB ||
            animNum == (int)Animation.AttackC;
    }

    /// <summary>
    /// [오브젝트 풀링(Pooling) 기반 파티클 최적화]: 이펙트를 매번 Instantiate/Destroy 하지 않고, 메모리에 있는 캐싱 객체의 위치만 옮겨 재사용(GC 발생 0%)합니다.
    /// </summary>
    //public void PlayEffect(ParticleSystem excuteEffect, float x, float y, float z)
    //{
    //    if (excuteEffect == null) return;
    //    Vector3 effectPos = new Vector3(x, y, z);
    //    excuteEffect.transform.position = effectPos;
    //    excuteEffect.Play();
    //}

    // 현재 유니티 애니메이터가 공격 관련 모션을 재생 중인지 확인합니다. (트랜지션 중 방어용)
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

    // 게임 시작 시 기획자가 지정한 안전한 최초 스폰 지점으로 보스를 강제 텔레포트합니다.
    public void Spawn()
    {
        if (spawnPos == null)
            return;

        // 월드 Z축 보정: 횡스크롤 게임의 원근감(Perspective) 오류를 막기 위해 Z축을 절대 고정합니다.
        Vector3 spawnPosition = new Vector3(spawnPos.position.x, spawnPos.position.y, worldZPos.position.z);

        transform.position = spawnPosition;
    }

    // 유휴(Idle) 상태 진입 시 이전 행동의 찌꺼기 변수들을 초기화합니다.
    public virtual void InitCurTime_Idle()
    {
        curTime_Idle = 0f;
        cachedLocomotionSpeed = -999f;
        cachedLocomotionAnim = -1;
        cachedLocomotionFacing = curFacing;
    }

    // 자식 클래스에서 오버라이드하여 플레이어 위치 기반 주변 배회 좌표를 설정하도록 설계된 껍데기 메서드입니다.
    public virtual void SetRandomPos()
    {
        if (playerPos == null) return;
    }

    /// <summary>
    /// [최적화 아키텍처: 더티 플래그(Dirty Flag) 패턴]
    /// 매 틱(Tick)마다 이동 방향이 계산될 때, 이전 프레임과 논리적 상태가 동일하다면 엔진 통신을 생략(Bypass)하여 마샬링 병목을 완벽히 제거합니다.
    /// </summary>
    protected void SyncLocomotionAnim(float moveDirX)
    {
        int targetAnim;
        float targetSpeed; // 애니메이션 보간 블렌드 트리 제어용 파라미터 (-1: 후진, 0: 대기, 1: 전진)

        if (moveDirX == 0f)
        {
            targetAnim = (int)Animation.Idle;
            targetSpeed = 0f;
        }
        else
        {
            targetAnim = (int)Animation.Walking;
            float facingDir = (curFacing == Facing.Right) ? 1f : -1f; // 시선 벡터 (우 1, 좌 -1)
            targetSpeed = moveDirX * facingDir; // 1D 수학 내적 응용: (이동방향 * 시선방향) 곱셈이 양수면 앞걷기, 음수면 뒷걸음질 애니메이션이 섞이게 됨
        }

        // 상태 오염 검증: 애니메이션 번호, 바라보는 방향, 이동 속도의 배율 3가지가 모두 이전 프레임과 똑같다면 렌더링 호출을 생략합니다.
        if (cachedLocomotionAnim == targetAnim &&
            cachedLocomotionFacing == curFacing &&
            Mathf.Abs(cachedLocomotionSpeed - targetSpeed) < 0.01f)
            return;

        // 값이 다를 때만(Edge Trigger 방식) 단 1회 엔진으로 렌더링 명령을 쏴줍니다.
        SyncFacingWithAnim(targetAnim);
        if (anim != null) anim.SetFloat("MoveForward", targetSpeed);

        // 메모리 동기화 갱신
        cachedLocomotionAnim = targetAnim;
        cachedLocomotionFacing = curFacing;
        cachedLocomotionSpeed = targetSpeed;
    }

    /// <summary>
    /// 플레이어가 보스 기준 어느 방향에 있는지 1차원 방향 벡터(-1 또는 1)로 변환하여 AI 연산의 수학적 단순화를 제공합니다.
    /// </summary>
    protected float GetDirectionToPlayer()
    {
        // 거리가 완벽하게 겹쳐 0일 경우 부동소수점 예외를 막기 위해 현재 바라보는 방향을 유지합니다.
        if (Mathf.Approximately(Distance, 0f))
            return curFacing == Facing.Right ? 1f : -1f;

        return Mathf.Sign(Distance);
    }

    /// <summary>
    /// 루주후드와 같은 원거리 카이팅 AI가 플레이어와 거리를 벌리며 뒤로 도약하다가 무대 밖(Ground Bounds)으로 이탈하는 현상을 수학적으로 방어(Clamping)합니다.
    /// </summary>
    protected float BlockMoveOutsideGround(float moveDirX, float edgePadding)
    {
        if (ground == null || Mathf.Approximately(moveDirX, 0f))
            return moveDirX;

        float minX = groundXMin + edgePadding;
        float maxX = groundXMax - edgePadding;
        float curX = transform.position.x;

        // 왼쪽 한계선에 닿았는데 계속 왼쪽으로 가려 하거나, 오른쪽 한계선에 닿았는데 오른쪽으로 가려 하면 이동력을 0으로 몰수합니다.
        if (moveDirX < 0f && curX <= minX) return 0f;
        if (moveDirX > 0f && curX >= maxX) return 0f;

        return moveDirX;
    }

    // 배회(Idling)와 걷기(Walking)를 명확히 나누는 서브 스테이트 머신 구현 지시용 추상 메서드
    public abstract void IdleMove();

    // 행동 트리가 조립 후 저장된 공격 타입과 콜라이더 문자열 ID 반환 API
    public AttackType GetAttackType() { return attackType; }
    public string GetAttackId() { return attackId; }

    // 공격 패턴 시작 시 FSM(상태 머신)이 호출하여 BT 루트 노드를 받아가는 API
    public Node GetAttackBT()
    {
        SetAttackContext(SelectAttack());
        return GetAttackNode(attackType) ?? CreateEmptyPatternNode();
    }

    /// <summary>
    /// 행동 트리(BT) 공용 노드용 유틸리티: 보스가 목표 좌표(pos)까지 speed의 속도로 추격하며 도달하면 완료를 반환합니다.
    /// </summary>
    protected NodeState Chase(Vector3 pos, float speed, int animNum)
    {
        UpdateFacing();
        if (!IsAnimationReady(animNum)) // 추격 걷기 모션이 렌더링 시작될 때까지 제자리 대기
        {
            Move(0, 0, 0);
            return NodeState.Running;
        }

        // 플레이어와의 절대 거리가 목표 타겟 사거리(pos.x) 밖이라면 거리를 좁힙니다.
        if (Math.Abs(Distance) > pos.x)
        {
            Move(Math.Sign(Distance) * speed, 0, 0);
            return NodeState.Running;
        }

        // 사거리 내 진입 시 정지
        Move(0, 0, 0);
        chaseDone = true;
        return NodeState.Success;
    }

    /// <summary>
    /// 행동 트리 사이클이 한 번 종료되거나 강제 취소되었을 때, 다음 공격에서 논리가 꼬이지 않도록 모든 플래그 스위치를 꺼버리는 청소(Clean-up) 메서드입니다.
    /// </summary>
    public virtual void LogicInit()
    {
        telegraphDrawer?.StopSignal(); // 이전 공격의 링과 정점 연출이 다음 공격에 남지 않게 정리
        chaseDone = false;
        telegraphExcuted = false;
        attackDone = false;
        isParryCanceled = false;
        parryCount = 0;
        comboStep = 0;
        ultimateAttack?.Reset();
        isGroggyAnimDone = false;
        StopGroggySfx(); // 실제 정지 요청 없이 플래그만 초기화해 재생 채널을 잃어버리지 않게 정리
        ResetPatternState();
    }

    // 궁극기 전용 BT 반환
    public Node GetUltimateBT() { return ultimateAttack ?? CreateEmptyPatternNode(); }

    // 그로기 발동 조건 검사 (기본적으로 누적 패링 3회 시 발동되나 자식 클래스에서 오버라이드 가능)
    public virtual bool CanTransitionToGroggy() { return parryCount >= 3; }

    /// <summary>
    /// 그로기 상태가 정상 완료되거나 다른 상태로 강제 전환될 때,
    /// 그로기 상태가 소유한 반복 SFX를 동일한 ID로 종료합니다.
    /// </summary>
    public void StopGroggySfx()
    {
        if (!isGroggySfxPlaying)
            return;

        EventBus<StopControlledSfxEvent>.Publish(
            new StopControlledSfxEvent(
                $"{GetInstanceID()}_Groggy",
                groggySfxFadeOutDuration));

        isGroggySfxPlaying = false;
    }

    /// <summary>
    /// 보스가 그로기(무력화) 상태에 빠졌을 때 애니메이션과 그로기 추가 데미지(Multiplier)를 통제합니다.
    /// </summary>
    public NodeState PlayAnimGroggy_Time(int num)
    {
        if (!isGroggyAnimDone)
        {
            // 애니메이터가 실제 Groggy 상태로 전환되기 전에는 SFX를 시작하지 않습니다.
            if (!IsAnimationReady(num)) return NodeState.Running;

            // Groggy 애니메이션이 처음 재생되는 시점에만 루프 SFX를 시작합니다.
            if (!isGroggySfxPlaying && groggySfx != null)
            {
                // 그로기 반복음에 Inspector에서 설정한 볼륨을 적용한다.
                EventBus<StartControlledSfxEvent>.Publish(
                    new StartControlledSfxEvent(
                        $"{GetInstanceID()}_Groggy",
                        groggySfx,
                        volume: groggySfxVolume,
                        loop: true));

                isGroggySfxPlaying = true;
            }

            // 그로기 지정 시간(groggyDuration)동안 무력화 모션 재생
            if (PlayAnim_Time(num, groggyDuration) == NodeState.Success)
            {
                isGroggyAnimDone = true;

                // Groggy 모션이 끝나고 Idle 기상 모션으로 넘어가기 직전에 루프 SFX를 종료합니다.
                StopGroggySfx();

                bossStatus?.SetGroggyDamageMultiplierActive(false); // 무력화 해제 직전 피격 추가 배율 보너스 종료
                return NodeState.Running;
            }
        }
        else if (PlayAnim_Time((int)Animation.Idle, 1) == NodeState.Success)
        {
            // 그로기 이후 기상 시, 즉시 폭주(Enranged) 상태로 돌입하여 난이도를 올립니다.
            isEnranged = true;
            curEnrangedAtkSpeed = enrangedAtkSpeed_Mul;
            return NodeState.Success;
        }

        return NodeState.Running;
    }

    // Update 루프에서 폭주 상태 지속 시간을 카운팅하여 일정 시간 이후 정상 상태로 복원시킵니다.
    public void EnrangedTimer()
    {
        curEnrangedTime += Time.deltaTime;
        if (curEnrangedTime < durationEnranged) return;

        isEnranged = false;
        curEnrangedTime = 0f;
        curEnrangedAtkSpeed = 1f;
    }

    // 애니메이션의 루트 모션 위치 덮어쓰기가 필요할 경우 자식 클래스에서 사용하는 콜백
    public virtual void OnAnimatorMoveCallback() { }

    /// <summary>
    /// [안전장치]: 기획 데이터 상 바닥(Ground) 콜라이더가 누락되어 런타임 NullReferenceException이 터지는 것을 막기 위해,
    /// 누락 시 보스의 이동 한계를 수학적 무한대(Infinity)로 대체하여 게임이 뻗지 않도록 하는 Fallback 로직입니다.
    /// </summary>
    private void InitMoveBounds()
    {
        if (ground != null)
        {
            groundXMin = ground.bounds.min.x;
            groundXMax = ground.bounds.max.x;
        }
        else
        {
            groundXMin = float.NegativeInfinity;
            groundXMax = float.PositiveInfinity;
        }
    }
    #endregion
}
