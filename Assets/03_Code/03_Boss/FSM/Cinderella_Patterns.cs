using System.Collections.Generic;
using System;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// [시스템 아키텍처: 구체화된 보스 패턴 (Concrete Implementation)]
/// BossPatternBase가 제공하는 로우 레벨 API(물리, 애니메이션, 마샬링)를 활용하여, 
/// 신데렐라 고유의 패턴(발차기, 파편, 점프 찍기, 궁극기)을 행동 트리(Behavior Tree)로 조립하는 'AI 두뇌' 클래스입니다.
/// </summary>
public class Cinderella_Patterns : BossPatternBase
{

    #region [1. 인스펙터 (기획 데이터 주입부)]
    [Header("Test")]
    [SerializeField] private ExcuteAttackType_InGame excuteAttackType_InGame; // 디버깅 시 특정 패턴(A, B, C)만 강제로 반복하게 만드는 제어 스위치

    [Header("SFX")]
    [SerializeField] private List<AudioClip> SFX_Punishments;

    // 패링 시 현재 기합음만 식별해 중단하기 위한 보스별 고유 ID입니다.
    private string punishmentSfxId => $"{GetInstanceID()}_Punishment";

    // [데이터 주도적 설계 (Data-Driven Design)]: 
    // 프로그래머의 코드 수정 없이 기획자가 인스펙터에서 보스의 거리, 템포, 딜레이를 직접 조율할 수 있도록 변수를 캡슐화했습니다.
    [Header("AttackA 상태 (Kick)")]
    [SerializeField] private float A_ChaseSpeed;        // 타겟을 향해 다가가는 이동 속도
    [SerializeField] private Vector3 A_ChasePos;        // 공격을 시작하기 위한 최적의 사거리 (X축 거리)
    [HideInInspector][SerializeField] private float A_telegraphTime; // (마이그레이션) 구버전 시간 기반 사전신호 변수
    [Tooltip("공격 발동 시간")][SerializeField] private float A_attackDuration;
    [Tooltip("후딜 시간")][SerializeField] private float A_postAtkDelay;
    [Tooltip("패링 시 보스 경직 시간")][SerializeField] private float A_parryStunDuration;

    [Header("AttackB 상태 (Spin Shard)")]
    [SerializeField] private float B_ChaseSpeed;
    [SerializeField] private Vector3 B_ChasePos;
    [HideInInspector][SerializeField] private float B_telegraphTime;
    [Tooltip("후딜 시간")][SerializeField] private float B_postAtkDelay;

    [Header("AttackC 상태 (Jump Slam)")]
    [SerializeField] private float C_ChaseSpeed;
    [SerializeField] private Vector3 C_ChasePos;
    [Tooltip("후딜 시간")][SerializeField] private float C_postAtkDelay;

    [Header("Ultimate 상태")]
    [SerializeField] private float UltimateChaseSpeed;
    [SerializeField] private Vector3 UltimateChasePos;
    [HideInInspector][SerializeField] private float ULTI_telegraphTime1;
    [HideInInspector][SerializeField] private float ULTI_telegraphTime2;
    [HideInInspector][SerializeField] private float ULTI_telegraphTime3;

    [Header("Cinderella Idle")]
    [Tooltip("순찰 시 목적지 무작위 오차 범위")][Min(0)][SerializeField] private float move_idleRange;
    [Tooltip("순찰 시 플레이어와 유지할 기본 거리")][Min(3)][SerializeField] private float move_idlePos;
    #endregion

    #region [2. BT 노드 및 특수 상태 캐싱]
    // [메모리 최적화]: 행동 트리의 Node 객체들을 매 공격마다 new로 생성하면 심각한 가비지 컬렉션(GC) 스파이크가 발생합니다.
    // 이를 막기 위해 게임 시작 시점에 단 1회만 메모리에 조립(Build)하여 캐싱해 두고 재사용합니다.
    private Node kickAttack;
    private Node spinShardAttack;
    private Node jumpSlamAttack;

    // [AI 상태 기억]: 연속으로 똑같은 공격을 두 번 시전하지 않도록 이전 공격 타입을 기억하는 변수입니다.
    private AttackType beforeType = AttackType.C;

    // [AttackC (Jump Slam) 전용 루트 모션 물리 통제용 변수들]
    private bool isJumping = false;             // 현재 보스가 공중에 떠서 물리를 무시해야 하는가?
    private Vector3 jumpStartPos;               // 도약 시작점 스냅샷
    private Vector3 jumpTargetPos;              // 도달해야 할 목표점 스냅샷
    private float currentJumpProgress = 0f;     // 애니메이션 커브에서 읽어온 진행도(0.0 ~ 1.0)

    // [순찰(Idle) 상태 제어용 변수]
    // 루주후드(실시간 거리유지)와 달리, 신데렐라는 랜덤한 특정 '목표점'을 찍고 그곳까지 걸어가는 방식을 사용합니다.
    private Vector3 RandomPos;
    private bool hasArrived = false; // 목적지에 도달하여 대기 중인가?

    // [Template Method 구현]: 점프 중일 때 베이스 클래스(BossPatternBase)에게 "나 지금 점프 중이니 기본 중력을 꺼줘!" 라고 알립니다.
    protected override bool IsPhysicsControlOverridden => isJumping;
    #endregion

    #region [3. 오버라이드 (템플릿 메서드 구현부)]
    /// <summary>
    /// [팩토리 패턴(Factory Pattern) 진입점]: Awake/Init 시점에 단 한 번 호출되어 보스의 모든 행동 트리를 메모리에 조립합니다.
    /// </summary>
    protected override void BuildPatterns()
    {
        kickAttack = BuildKickAttack();
        spinShardAttack = BuildSpinShardAttack();
        jumpSlamAttack = BuildJumpSlamAttack();
        ultimateAttack = BuildUltimateAttack();
    }

    /// <summary>
    /// [AI 의사결정 로직]: 다음에 실행할 공격 타입을 논리적으로 선택합니다.
    /// </summary>
    protected override AttackType SelectAttack()
    {
        switch (excuteAttackType_InGame)
        {
            case ExcuteAttackType_InGame.A: return AttackType.A;
            case ExcuteAttackType_InGame.B: return AttackType.B;
            case ExcuteAttackType_InGame.C: return AttackType.C;
            case ExcuteAttackType_InGame.ALL:
                AttackType selectedType;
                // [의사 난수(Pseudo-Random) 알고리즘]: 이전 패턴(beforeType)과 다른 패턴이 나올 때까지 난수 생성을 반복하여 패턴의 다채로움을 보장합니다.
                do { selectedType = (AttackType)Random.Range(0, 3); }
                while (selectedType == beforeType);

                beforeType = selectedType;
                return selectedType;
            default: return AttackType.A;
        }
    }

    /// <summary>
    /// 선택된 공격 타입에 매칭되는 캐싱된 BT 노드를 반환합니다. (C# 8.0 Switch Expression 도입으로 O(1) 분기 속도 보장)
    /// </summary>
    protected override Node GetAttackNode(AttackType selectedAttackType)
    {
        return selectedAttackType switch
        {
            AttackType.A => kickAttack,
            AttackType.B => spinShardAttack,
            AttackType.C => jumpSlamAttack,
            _ => null
        };
    }

    /// <summary>
    /// 공격이 캔슬되거나 강제 전환될 때, 신데렐라 고유의 오염된 상태(예: 공중에 떠있는 플래그)를 안전하게 초기화합니다.
    /// </summary>
    protected override void ResetPatternState()
    {
        isJumping = false;
    }

    /// <summary>
    /// [다형성(Polymorphism) 구현]: 베이스 클래스의 지시를 받아 신데렐라 전용 UI(RingDrawer)에 애니메이터 연동 신호를 보냅니다.
    /// </summary>
    protected override NodeState PlayTelegraph(TelegraphType type)
    {
        // 여기서는 링을 활성화하고 Animator 참조를 넘겨 준비만 한다.
        // 실제 수축 속도와 정점은 현재 공격 Animation Clip의 ParryTelegraphProgress 커브 및 EnableParry 이벤트가 결정한다.
        if (telegraph != null) telegraph.SetActive(true);
        telegraphDrawer?.BeginSignal(anim);
        telegraphExcuted = true;
        return NodeState.Success;
    }

    public override void InitCurTime_Idle()
    {
        base.InitCurTime_Idle(); // 베이스 클래스의 기본 초기화 실행
        hasArrived = false;

        // [안전장치(Fail-safe)]: 오차 범위(Range)가 유지 거리(Pos)보다 크면 보스가 플레이어를 관통하여 
        // 반대편으로 가버리는 기획적 논리 오류가 발생하므로 코드 단에서 강제 보정합니다.
        if (move_idleRange >= move_idlePos)
            move_idleRange = move_idlePos - 0.5f;
    }

    /// <summary>
    /// [수학적 공간 제어]: 보스가 배회(Idle)할 무작위 1D 좌표를 계산합니다.
    /// </summary>
    public override void SetRandomPos()
    {
        if (playerPos == null)
        {
            RandomPos = transform.position;
            return;
        }

        // 1. 최소 유지거리(move_idlePos)에 무작위 오차(move_idleRange)를 더해 절대적인 거리를 구합니다.
        float element = move_idlePos + Random.Range(-move_idleRange, move_idleRange);

        // 2. [방향성 내적 응용]: Math.Sign을 이용해 플레이어가 내 왼쪽(-1)에 있으면 플레이어의 오른쪽(+) 좌표를, 반대면 반대 좌표를 타겟으로 삼습니다.
        if (Math.Sign(Distance) <= 0) RandomPos = new Vector3(playerPos.position.x + element, 0, 0);
        else RandomPos = new Vector3(playerPos.position.x - element, 0, 0);

        // 3. 계산된 좌표가 무대를 벗어나지 않도록 Ground Collider의 Min/Max로 클램프(강제 범위 고정)시킵니다.
        if (ground != null)
            RandomPos.x = Mathf.Clamp(RandomPos.x, groundXMin, groundXMax);
    }

    /// <summary>
    /// [서브 스테이트 머신]: 단순한 이동 메서드가 아니라, 목적지 도착 전(Phase 1: 걷기)과 도착 후(Phase 2: 대기)를 명확히 분리하여, 목적지에서 앞뒤로 덜덜 떨리는 지터링(Jittering) 버그를 원천 차단합니다.
    /// </summary>
    public override void IdleMove()
    {
        UpdateFacing();
        curTime_Idle += Time.deltaTime;

        if (!hasArrived) // [Phase 1: 이동 연산 진행 중]
        {
            float distToTarget = Mathf.Abs(RandomPos.x - transform.position.x);

            // 도착 판정 2가지: 1) 목표점에 오차범위(0.5) 내로 도달했는가? 2) 플레이어가 내 앞을 가로막고 있는가(Bodyblock)?
            if (distToTarget <= 0.5f || (Math.Sign(Distance) == Math.Sign(RandomPos.x - transform.position.x) && distToTarget > Mathf.Abs(Distance)))
            {
                hasArrived = true;      // 상태 락(Lock) 발동
                Move(0, 0, 0);          // 물리 연산 정지
                SyncLocomotionAnim(0f); // 애니메이터 시각적 정지
            }
            else // 아직 도착하지 않았다면
            {
                // 목적지의 방향(1 또는 -1)을 추출하여 이동 로직과 애니메이션 렌더링에 데이터를 주입(Push)합니다.
                float moveDirX = (transform.position.x < RandomPos.x) ? 1f : -1f;
                Move(moveDirX * move_idleSpeed, 0, 0);
                SyncLocomotionAnim(moveDirX);
            }
        }

        // [Phase 2: 이동 완료 후 단순 대기] - 불필요한 거리 연산을 전면 생략하여 CPU 오버헤드를 낮춥니다.
        if (curTime_Idle > idleDurationTime)
            SetStateDone(true);
    }
    #endregion

    #region [4. 행동 트리(BT) 조립 공장]

    /// <summary>
    /// [BT 조립 아키텍처]: 모든 단일 공격은 엄격한 5단계의 시퀀스로 캡슐화됩니다.
    /// 1) 패링 피격 확인 -> 2) 사거리 내 타겟 추격 -> 3) 사전신호(Telegraph) 발생 -> 4) 실제 타격 및 판정 -> 5) 후딜레이 대기
    /// </summary>
    private Node BuildKickAttack()
    {
        return new Selector(new List<Node>
        {
            // [1단계: 방어(패링) 기믹 처리] - 진행도를 내부에서 기억하는 StatefulSequence를 사용하여 프레임이 넘어가도 루프를 유지합니다.
            new StatefulSequence(new List<Node>
            {
                new ConditionLeaf(() => isParryed), // 플레이어의 패링이 성공했는가? (조건 통과 시 하위 로직 실행)
                new Leaf(() =>
                {
                    PublishParryImpact(); // 카메라 흔들림 및 불릿타임/히트스탑 연출
                    StopPunishmentSfx();  // 패링이 확정된 즉시 현재 공격의 기합음을 중단
                    return NodeState.Success;
                }),
                new Leaf(() => PlayAnim_Speed((int)Animation.Parry, A_parryStunDuration)), // 지정된 시간만큼 스턴 모션 재생
                new Leaf(() =>
                {
                    Parryed(); // 콜라이더 및 플래그 초기화
                    SetStateDone(true); // 공격이 강제 종료되었음을 FSM에 보고
                    return NodeState.Success;
                })
            }),
            
            // [2~5단계: 정상 공격 시퀀스] - 이전 단계가 완료(Done = true)되어야만 다음 단계의 Selector가 해제되는 직렬 구조입니다.
            new Sequence(new List<Node>
            {
                // [2. 추격]: chaseDone 플래그가 true가 될 때까지 Chase 함수를 매 프레임 반복 호출합니다.
                new Selector(new List<Node>
                {
                    new ConditionLeaf(() => chaseDone),
                    new Leaf(() => Chase(A_ChasePos, A_ChaseSpeed, (int)Animation.Chase))
                }),
                // [3. 사전신호]: 리팩토링된 데이터 주도적 동기화를 통해 애니메이터에게 UI 제어를 넘깁니다.
                new Selector(new List<Node>
                {
                    new ConditionLeaf(() => telegraphExcuted),
                    new Sequence(new List<Node>
                    {
                        new Leaf(() => //기합 소리 재생
                        {
                            PlayPunishmentSfx();
                            return NodeState.Success;
                        }),
                        new Leaf(() => PlayTelegraph(TelegraphType.RingDrawer))
                    })
                }),
                // [4. 타격]: 실제 공격 애니메이션을 재생합니다.
                new Selector(new List<Node>
                {
                    new ConditionLeaf(() => attackDone),
                    new StatefulSequence(new List<Node>
                    {
                        new Leaf(() => { UpdateFacing(); return NodeState.Success; }), // 타격 직전 마지막으로 방향 보정
                        new Leaf(() => PlayAnim_Speed((int)Animation.AttackA, A_attackDuration)),
                        new Leaf(() =>
                        {
                            attackDone = true;
                            return NodeState.Success;
                        })
                    })
                }),
                // [5. 후딜레이]: 공격이 끝난 후 빈틈(Idle)을 제공합니다.
                new Sequence(new List<Node>
                {
                    new Leaf(() =>
                    {
                        UpdateFacing();
                        return PlayAnim_Time((int)Animation.Idle, A_postAtkDelay);
                    }),
                    new Leaf(() => SetStateDone(true)) // 최종적으로 FSM에 공격 사이클 종료 보고
                })
            })
        });
    }

    // (SpinShardAttack의 구조도 발차기와 100% 동일한 5단계 원리로 조립됩니다.)
    private Node BuildSpinShardAttack()
    {
        return new Selector(new List<Node>
        {
            new StatefulSequence(new List<Node>
            {
                new ConditionLeaf(() => isParryed),
                new Leaf(() =>
                {
                    PublishParryImpact();
                    StopPunishmentSfx();
                    return NodeState.Success;
                }),
                new Leaf(() => PlayAnim_Speed((int)Animation.Parry, 0f)),
                new Leaf(() =>
                {
                    Parryed();
                    //bossStatus?.TakeDamage(30); // 기믹: 이 공격을 패링당하면 보스가 스스로 고정 데미지를 입음
                    SetStateDone(true);
                    return NodeState.Success;
                })
            }),
            new Sequence(new List<Node>
            {
                new Selector(new List<Node>
                {
                    new ConditionLeaf(() => chaseDone),
                    new Leaf(() => Chase(B_ChasePos, B_ChaseSpeed, (int)Animation.Chase))
                }),
                new Selector(new List<Node>
                {
                    new ConditionLeaf(() => telegraphExcuted),
                    new Sequence(new List<Node>
                    {
                        new Leaf(() => //기합 소리 재생
                        {
                            PlayPunishmentSfx();
                            return NodeState.Success;
                        }),
                        new Leaf(() => PlayTelegraph(TelegraphType.RingDrawer))
                    })
                }),
                new Selector(new List<Node>
                {
                    new ConditionLeaf(() => attackDone),
                    new StatefulSequence(new List<Node>
                    {
                        new Leaf(() => { UpdateFacing(); return NodeState.Success; }),
                        new Leaf(() => PlayAnim_Speed((int)Animation.AttackB, 0f)),
                        new Leaf(() =>
                        {
                            if (!isParryCanceled)
                                EventBus<OnShardHitBoxEvent>.Publish(new OnShardHitBoxEvent(transform)); // 공격 성공 시 장판 생성 이벤트 발송
                            attackDone = true;
                            return NodeState.Success;
                        })
                    })
                }),
                new Sequence(new List<Node>
                {
                    new Leaf(() =>
                    {
                        UpdateFacing();
                        return PlayAnim_Time((int)Animation.Idle, B_postAtkDelay);
                    }),
                    new Leaf(() => SetStateDone(true))
                })
            })
        });
    }

    private Node BuildJumpSlamAttack()
    {
        return new Selector(new List<Node>
        {
            new Sequence(new List<Node>
            {
                new Selector(new List<Node>
                {
                    new ConditionLeaf(() => chaseDone),
                    new Leaf(() => Chase(C_ChasePos, C_ChaseSpeed, (int)Animation.Chase))
                }),
                new Selector(new List<Node>
                {
                    new ConditionLeaf(() => attackDone),
                    new StatefulSequence(new List<Node>
                    {
                        // [루트 모션 제어 진입점]: 단순 애니메이션 재생이 아니라 좌표를 조작하는 특수 노드 진입
                        new Leaf(() =>
                        {
                            Vector3 targetPos = playerPos != null ? playerPos.position : transform.position;
                            return DynamicJumpSlam(targetPos, (int)Animation.AttackC);
                        }),
                        new Leaf(() =>
                        {
                            attackDone = true;
                            return NodeState.Success;
                        })
                    })
                }),
                new Sequence(new List<Node>
                {
                    new Leaf(() =>
                    {
                        UpdateFacing();
                        return PlayAnim_Time((int)Animation.Idle, C_postAtkDelay);
                    }),
                    new Leaf(() => SetStateDone(true))
                })
            })
        });
    }

    /// <summary>
    /// [다단계 복합 상태 제어]: 궁극기는 여러 번의 타격(3연타)으로 이루어지므로, comboStep 변수를 활용하여
    /// 프레임이 평가(Tick)될 때마다 처음부터 리셋되지 않고 현재 콤보 단계에서 안전하게 재개(Resume)되도록 설계되었습니다.
    /// </summary>
    private Node BuildUltimateAttack()
    {
        return new Selector(new List<Node>
        {
            new StatefulSequence(new List<Node>
            {
                new ConditionLeaf(() => isParryed),
                new Leaf(() =>
                {
                    PublishParryImpact();
                    StopPunishmentSfx();
                    return NodeState.Success;
                }),
                new Leaf(() => PlayAnim_Speed((int)Animation.Parry, 0f)),
                new Leaf(() =>
                {
                    Parryed();
                    parryCount++;   // 패링 성공 누적 카운트 증가 (3회 도달 시 그로기)
                    comboStep++;    // 현재 공격 캔슬 및 다음 콤보 단계로 강제 스킵
                    if(comboStep >= 3) //parryCount가 2이하에 3번째 패링 시 1프레임 더 돌아서 PlayTelegraph가 실행되는거 방어
                    {
                        attackDone = true;
                        if(!CanTransitionToGroggy()) SetStateDone(true);
                    }
                    return NodeState.Success;
                })
            }),
            new Sequence(new List<Node>
            {
                // 콜라이더 시스템이 궁극기 데미지를 참조할 수 있도록 전용 컨텍스트(D) 세팅
                new Leaf(() => { SetAttackContext(AttackType.D); return NodeState.Success; }),
                new Selector(new List<Node>
                {
                    new ConditionLeaf(() => chaseDone),
                    new Leaf(() => Chase(UltimateChasePos, UltimateChaseSpeed, (int)Animation.Chase))
                }),
                // [단축 최적화]: 3가지의 다른 궁극기 모션도 모두 애니메이터 커브에 시간을 종속시켰으므로 단 한 줄의 신호 노드만 사용합니다.
                new Selector(new List<Node>
                {
                    new ConditionLeaf(() => telegraphExcuted),
                    new Sequence(new List<Node>
                    {
                        new Leaf(() => //기합 소리 재생
                        {
                            PlayPunishmentSfx();
                            return NodeState.Success;
                        }),
                        new Leaf(() => PlayTelegraph(TelegraphType.RingDrawer))
                    })
                }),
                new Selector(new List<Node>
                {
                    new ConditionLeaf(() => attackDone),
                    new StatefulSequence(new List<Node>
                    {
                        new Leaf(() => { UpdateFacing(); return NodeState.Success; }),
                        new Selector(new List<Node>
                        {
                            new ConditionLeaf(() => comboStep > 0), // comboStep 0일 때 1타 실행
                            new Sequence(new List<Node>
                            {
                                new Leaf(() => PlayAnim_Speed((int)Animation.Ultimate1, 0f)),
                                new Leaf(() => { EventBus<AttackFinishEvent>.Publish(default); return NodeState.Success; }),
                                new Leaf(() =>
                                {
                                    comboStep++; // 다음 타격으로 인덱스 이동
                                    telegraphExcuted = false; // 신호 플래그 초기화
                                    return NodeState.Success;
                                })
                            })
                        }),
                        new Leaf(() => { UpdateFacing(); return NodeState.Success; }),
                        new Selector(new List<Node>
                        {
                            new ConditionLeaf(() => comboStep > 1), // comboStep 1일 때 2타 실행
                            new Sequence(new List<Node>
                            {
                                new Leaf(() => PlayAnim_Speed((int)Animation.Ultimate2, 0f)),
                                new Leaf(() => { EventBus<AttackFinishEvent>.Publish(default); return NodeState.Success; }),
                                new Leaf(() =>
                                {
                                    comboStep++;
                                    telegraphExcuted = false;
                                    return NodeState.Success;
                                })
                            })
                        }),
                        new Leaf(() => { UpdateFacing(); return NodeState.Success; }),
                        new Selector(new List<Node>
                        {
                            new ConditionLeaf(() => comboStep > 2), // comboStep 2일 때 3타 실행
                            new Sequence(new List<Node>
                            {
                                new Leaf(() => PlayAnim_Speed((int)Animation.Ultimate3, 0f)),
                                new Leaf(() => { EventBus<AttackFinishEvent>.Publish(default); return NodeState.Success; }),
                                new Leaf(() =>
                                {
                                    comboStep++;
                                    telegraphExcuted = false;
                                    return NodeState.Success;
                                })
                            })
                        }),
                        new Leaf(() =>
                        {
                            telegraphExcuted = false;
                            attackDone = true;
                            return SetStateDone(true); // 3연타 최종 완료
                        })
                    })
                })
            })
        });
    }
    #endregion

    #region [5. 루트 모션 물리 제어 (Jump Slam)]

    /// <summary>
    /// [시간/상태 캡슐화]: 점프 애니메이션이 재생되는 동안의 상태 변화를 정규화된 시간(normalizedTime: 0.0 ~ 1.0) 단위로 정밀하게 통제합니다.
    /// </summary>
    private NodeState DynamicJumpSlam(Vector3 targetPos, int animNum)
    {
        // 1. [진입점 (Edge Trigger)]: 점프 애니메이션 명령이 떨어지는 첫 1프레임에만 진입하여 메모리 스냅샷을 찍습니다.
        if (!isJumping)
        {
            UpdateFacing();
            SyncFacingWithAnim(animNum);
            PlayPunishmentSfx();

            jumpStartPos = transform.position;  // 도약 위치 스냅샷 기록
            jumpTargetPos = targetPos;          // 착지해야 할 목표(플레이어) 위치 스냅샷 기록
            isJumping = true;                   // 상태 락(Lock) 발동: Base 클래스가 중력을 끄도록 유도
            currentJumpProgress = 0f;

            return NodeState.Running;
        }

        // 방어 코드
        if (anim == null) { isJumping = false; return NodeState.Success; }
        if (anim.IsInTransition(0)) return NodeState.Running;

        // 2. [진행 중]: 현재 애니메이션이 몇 % 진행되었는지 가져옵니다.
        animState = anim.GetCurrentAnimatorStateInfo(0);

        // 애니메이션 진행도 30%(0.3f) 지점: 보스의 발이 바닥을 강하게 찍는 찰나의 시점
        // attackType을 C_2로 변경(EventBus 발송)하여 파동 콜라이더(Hitbox)를 활성화하고 먼지 파티클을 재생하도록 유도합니다.
        if (animState.normalizedTime >= 0.3f && attackType != AttackType.C_2)
        {
            SetAttackContext(AttackType.C_2);
        }

        // 3. [종료점]: 애니메이션이 95% 이상 완료되어 착지가 끝났다면 점프 락을 해제하고 시퀀스 완료를 반환합니다.
        if (animState.normalizedTime >= 0.95f)
        {
            isJumping = false;
            SetAttackContext(AttackType.C);
            return NodeState.Success;
        }

        return NodeState.Running;
    }

    /// <summary>
    /// [루트 모션 탈취 (Root Motion Override) 최적화 기법]
    /// 유니티 엔진(C++)이 애니메이션 클립의 움직임 데이터를 실제 Transform에 반영하기 직전 단계에 개입하여, 
    /// 물리 엔진을 완전히 무시하고 프로그래머의 수학 공식대로 보스의 멱살을 잡아 좌표를 강제로 꽂아 넣는 강력한 콜백 함수입니다.
    /// </summary>
    public override void OnAnimatorMoveCallback()
    {
        if (anim == null || rb == null) return;

        // 점프 중이 아닐 때는 물리 엔진(Rigidbody)에게 100% 통제권을 돌려줍니다.
        if (!isJumping)
        {
            rb.MovePosition(rb.position);
            return;
        }

        // [점프 궤적 수학적 덮어쓰기 로직]

        // 1. X축(수평 이동 거리 보정): 
        // 애니메이터에 기획자가 박아넣은 곡선 커브("JumpProgress" 0~1)를 읽어옵니다.
        // 시작점과 목표점을 이 커브 비율로 선형 보간(Lerp)합니다. 이렇게 하면 타겟이 1m 앞이든 10m 앞이든 
        // 무조건 애니메이션 템포(초반엔 느리게, 찍을 땐 확 빠르게)와 100% 일치하는 완벽한 타격 프레임을 보장합니다.
        currentJumpProgress = anim.GetFloat("JumpProgress");
        float newX = Mathf.Lerp(jumpStartPos.x, jumpTargetPos.x, currentJumpProgress);

        // 2. Y축(수직 상승/하강 보정): 
        // 보스가 뛰어오르는 포물선 높이는 프로그래머가 계산하지 않고, 애니메이션 클립(.anim)이 원래 가지고 있던 
        // 이번 프레임의 Y축 순수 뼈대 변화량(deltaPosition.y)을 그대로 현재 좌표에 더해주어 시각적 자연스러움을 극대화합니다.
        float newY = transform.position.y + anim.deltaPosition.y;

        // 3. Z축(깊이): 3D 공간을 활용한 횡스크롤 게임이므로 렌더링 뎁스가 무너지지 않도록 기획된 절대 좌표(worldZPos)로 고정합니다.
        float curZ = worldZPos.position.z;

        // 조립된 최종 좌표를 Transform에 강제 대입 (Rigidbody Bypass)
        transform.position = new Vector3(newX, newY, curZ);
    }
    #endregion

    #region SFX 제어
    // 패턴 시작 시 리스트에서 하나를 선택해 제어형 SFX 채널로 재생합니다.
    // loop=false여도 SoundManager가 재생 완료를 감지해 풀로 자동 반납합니다.
    private void PlayPunishmentSfx()
    {
        if (SFX_Punishments == null || SFX_Punishments.Count == 0)
            return;

        AudioClip clip = SFX_Punishments[Random.Range(0, SFX_Punishments.Count)];

        EventBus<StartControlledSfxEvent>.Publish(
            new StartControlledSfxEvent(punishmentSfxId, clip, loop: false));
    }

    // 패링 성공 시 같은 ID로 재생 중인 기합음만 즉시 중단합니다.
    private void StopPunishmentSfx()
    {
        EventBus<StopControlledSfxEvent>.Publish(
            new StopControlledSfxEvent(punishmentSfxId));
    }
    #endregion
}
