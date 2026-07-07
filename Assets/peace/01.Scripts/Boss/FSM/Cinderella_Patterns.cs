using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// [시스템 아키텍처: 구체화된 보스 패턴 클래스]
/// BossPatternBase가 제공하는 API를 활용하여 신데렐라 고유의 패턴(발차기, 파편, 점프 찍기, 궁극기)을 행동 트리(BT)로 조립합니다.
/// </summary>
public class Cinderella_Patterns : BossPatternBase
{
    #region [1. 인스펙터 (기획 데이터 주입부)]
    [Header("Test")]
    [SerializeField] private ExcuteAttackType_InGame excuteAttackType_InGame; // 디버깅용 강제 패턴 지정 스위치

    // [설계 의도]: 각 공격 타입(A, B, C, 궁극기)마다 필요한 '거리, 속도, 시간' 데이터를 캡슐화하여 
    // 기획자가 프로그래머의 도움 없이 보스의 템포를 자유롭게 조절할 수 있도록 합니다.
    [Header("AttackA 상태 (Kick)")]
    [SerializeField] private float A_ChaseSpeed;
    [SerializeField] private Vector3 A_ChasePos;
    [SerializeField] private float A_telegraphTime;
    [Tooltip("공격 발동 시간")][SerializeField] private float A_attackDuration;
    [Tooltip("후딜 시간")][SerializeField] private float A_postAtkDelay;
    [Tooltip("패링 시 보스 경직 시간")][SerializeField] private float A_parryStunDuration;

    [Header("AttackB 상태 (Spin Shard)")]
    [SerializeField] private float B_ChaseSpeed;
    [SerializeField] private Vector3 B_ChasePos;
    [SerializeField] private float B_telegraphTime;
    [Tooltip("후딜 시간")][SerializeField] private float B_postAtkDelay;

    [Header("AttackC 상태 (Jump Slam)")]
    [SerializeField] private ParticleSystem waveEffect; // 바닥 찍기 파동 이펙트
    [SerializeField] private float C_ChaseSpeed;
    [SerializeField] private Vector3 C_ChasePos;
    [Tooltip("후딜 시간")][SerializeField] private float C_postAtkDelay;

    [Header("Ultimate 상태")]
    [SerializeField] private float UltimateChaseSpeed;
    [SerializeField] private Vector3 UltimateChasePos;
    [SerializeField] private float ULTI_telegraphTime1;
    [SerializeField] private float ULTI_telegraphTime2;
    [SerializeField] private float ULTI_telegraphTime3;
    #endregion

    #region [2. BT 노드 및 특수 상태 캐싱]
    // [설계 의도]: 행동 트리를 매번 new 키워드로 생성하면 가비지 컬렉션(GC)이 폭주합니다.
    // 이를 막기 위해 게임 시작 시점에 단 1회만 메모리에 노드를 조립해 두고(Caching), 런타임에는 꺼내 쓰기만 합니다.
    private Node kickAttack;
    private Node spinShardAttack;
    private Node jumpSlamAttack;

    // 무작위 공격 시, 직전 공격과 똑같은 패턴이 두 번 연속 나오지 않도록 기억하는 변수
    private AttackType beforeType = AttackType.C;

    // AttackC (Jump Slam) 전용 특수 상태 변수들
    private bool isJumping = false;
    private Vector3 jumpStartPos;
    private Vector3 jumpTargetPos;
    private float currentJumpProgress = 0f;

    // [Template Method 구현]: 점프 중일 때 베이스 클래스에게 "나 지금 점프 중이니까 기본 중력/물리 엔진 꺼줘!" 라고 알림
    protected override bool IsPhysicsControlOverridden => isJumping;
    #endregion

    #region [3. 오버라이드 (템플릿 메서드 구현부)]
    /// <summary>
    /// [Factory Pattern]: 게임 시작(Awake/Init) 시점에 호출되어 모든 행동 트리를 메모리에 조립합니다.
    /// </summary>
    protected override void BuildPatterns()
    {
        kickAttack = BuildKickAttack();
        spinShardAttack = BuildSpinShardAttack();
        jumpSlamAttack = BuildJumpSlamAttack();
        ultimateAttack = BuildUltimateAttack();
    }

    /// <summary>
    /// 다음에 실행할 공격 타입을 결정합니다.
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
                // [알고리즘]: 이전 패턴(beforeType)과 다른 패턴이 나올 때까지 난수 생성 반복
                do { selectedType = (AttackType)Random.Range(0, 3); }
                while (selectedType == beforeType);

                beforeType = selectedType;
                return selectedType;
            default: return AttackType.A;
        }
    }

    /// <summary>
    /// 선택된 공격 타입에 매칭되는 캐싱된 BT 노드를 반환합니다. (C# 8.0 Switch Expression 도입으로 O(1) 속도 보장)
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
    /// 공격이 캔슬되거나 상태가 전환될 때, 신데렐라 고유의 오염된 상태(점프 플래그 등)를 안전하게 초기화합니다.
    /// </summary>
    protected override void ResetPatternState()
    {
        isJumping = false;
    }
    #endregion

    #region [4. 행동 트리(BT) 조립 공장]

    /// <summary>
    /// [BT 조립 원리]: 모든 공격 로직은 5단계의 시퀀스로 이루어집니다.
    /// 1) 패링 확인 2) 타겟 추격 3) 사전신호(Telegraph) 4) 실제 공격 타격 5) 후딜레이(Idle)
    /// 각 단계는 Selector와 ConditionLeaf를 조합하여, 이전 단계가 완료(Done = true)되어야만 다음 단계로 넘어가도록 설계되었습니다.
    /// </summary>
    private Node BuildKickAttack()
    {
        return new Selector(new List<Node>
        {
            // [1단계: 방어(패링) 기믹 처리]
            new StatefulSequence(new List<Node>
            {
                new ConditionLeaf(() => isParryed), // 플레이어가 패링 버튼을 성공적으로 눌렀는가?
                new Leaf(() =>
                {
                    PublishParryImpact(); // 베이스 클래스의 카메라 흔들림 및 역경직(HitStop) 호출
                    return NodeState.Success;
                }),
                new Leaf(() => PlayAnim_Speed((int)Animation.Parry, A_parryStunDuration)), // 보스 스턴 애니메이션
                new Leaf(() =>
                {
                    Parryed(); // 콜라이더 회수 및 상태 초기화
                    SetStateDone(true); // 공격 완전히 종료 처리
                    return NodeState.Success;
                })
            }),
            
            // [2~5단계: 실제 공격 시퀀스]
            new Sequence(new List<Node>
            {
                // [2. 추격] chaseDone이 true가 될 때까지 Chase 함수 계속 호출
                new Selector(new List<Node>
                {
                    new ConditionLeaf(() => chaseDone),
                    new Leaf(() => Chase(A_ChasePos, A_ChaseSpeed, (int)Animation.Chase))
                }),
                // [3. 사전신호]
                new Selector(new List<Node>
                {
                    new ConditionLeaf(() => telegraphExcuted),
                    new Leaf(() => PlayTelegraph(A_telegraphTime))
                }),
                // [4. 타격]
                new Selector(new List<Node>
                {
                    new ConditionLeaf(() => attackDone),
                    new StatefulSequence(new List<Node>
                    {
                        new Leaf(() => { UpdateFacing(); return NodeState.Success; }),
                        new Leaf(() => PlayAnim_Speed((int)Animation.AttackA, A_attackDuration)),
                        new Leaf(() =>
                        {
                            attackDone = true;
                            return NodeState.Success;
                        })
                    })
                }),
                // [5. 후딜레이]
                new Sequence(new List<Node>
                {
                    new Leaf(() =>
                    {
                        UpdateFacing();
                        return PlayAnim_Time((int)Animation.Idle, A_postAtkDelay);
                    }),
                    new Leaf(() => SetStateDone(true)) // 최종 종료
                })
            })
        });
    }
    // (SpinShardAttack과 UltimateAttack도 동일한 5단계 원리로 조립됨. Ultimate는 콤보 스텝(comboStep) 변수로 3연타를 제어함)
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
                    return NodeState.Success;
                }),
                new Leaf(() => PlayAnim_Speed((int)Animation.Parry, 0f)),
                new Leaf(() =>
                {
                    Parryed();
                    bossStatus?.TakeDamage(30);
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
                    new Leaf(() => PlayTelegraph(B_telegraphTime))
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
                                EventBus<OnShardHitBoxEvent>.Publish(new OnShardHitBoxEvent(transform));
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
                new Sequence(new List<Node>()),
                new Selector(new List<Node>
                {
                    new ConditionLeaf(() => attackDone),
                    new StatefulSequence(new List<Node>
                    {
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
                    return NodeState.Success;
                }),
                new Leaf(() => PlayAnim_Speed((int)Animation.Parry, 0f)),
                new Leaf(() =>
                {
                    Parryed();
                    parryCount++;
                    comboStep++;
                    return NodeState.Success;
                })
            }),
            new Sequence(new List<Node>
            {
                new Leaf(() => { SetAttackContext(AttackType.D); return NodeState.Success; }),
                new Selector(new List<Node>
                {
                    new ConditionLeaf(() => chaseDone),
                    new Leaf(() => Chase(UltimateChasePos, UltimateChaseSpeed, (int)Animation.Chase))
                }),
                new Selector(new List<Node>
                {
                    new ConditionLeaf(() => telegraphExcuted),
                    new Leaf(() =>
                    {
                        float time;
                        if (comboStep == 0) time = ULTI_telegraphTime1;
                        else if (comboStep == 1) time = ULTI_telegraphTime2;
                        else time = ULTI_telegraphTime3;

                        return PlayTelegraph(time);
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
                            new ConditionLeaf(() => comboStep > 0),
                            new Sequence(new List<Node>
                            {
                                new Leaf(() => PlayAnim_Speed((int)Animation.Ultimate1, 0f)),
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
                            new ConditionLeaf(() => comboStep > 1),
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
                            new ConditionLeaf(() => comboStep > 2),
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
                            attackDone = true;
                            return SetStateDone(true);
                        })
                    })
                })
            })
        });
    }
    #endregion

    #region [5. 루트 모션 물리 제어 (Jump Slam)]
    /// <summary>
    /// [시간/상태 제어]: 점프 애니메이션이 재생되는 동안의 상태 변화를 프레임(정규화된 시간) 단위로 통제합니다.
    /// </summary>
    private NodeState DynamicJumpSlam(Vector3 targetPos, int animNum)
    {
        // 1. [진입점 (Edge Trigger)]: 점프를 막 시작했을 때 1회만 실행
        if (!isJumping)
        {
            UpdateFacing();
            SyncFacingWithAnim(animNum);

            jumpStartPos = transform.position;  // 도약 위치 스냅샷
            jumpTargetPos = targetPos;          // 착지할 목표 위치 스냅샷
            isJumping = true;                   // 상태 락(Lock)
            currentJumpProgress = 0f;

            return NodeState.Running;
        }

        if (anim == null) { isJumping = false; return NodeState.Success; }
        if (anim.IsInTransition(0)) return NodeState.Running;

        // 2. [진행 중]: 현재 애니메이션이 몇 % 진행되었는지(0.0 ~ 1.0) 가져옵니다.
        animState = anim.GetCurrentAnimatorStateInfo(0);

        // 애니메이션 진행도 30%(0.3f) 지점: 보스가 바닥을 찍는 시점
        // attackType을 C_2로 변경(EventBus 발송)하여 파동 콜라이더를 켜고 파티클을 재생하도록 유도합니다.
        if (animState.normalizedTime >= 0.3f && attackType != AttackType.C_2)
        {
            SetAttackContext(AttackType.C_2);
        }

        // 3. [종료점]: 애니메이션이 95% 이상 완료되면 점프 상태를 해제하고 성공 반환
        if (animState.normalizedTime >= 0.95f)
        {
            isJumping = false;
            SetAttackContext(AttackType.C);
            return NodeState.Success;
        }

        return NodeState.Running;
    }

    /// <summary>
    /// [루트 모션 탈취 (Root Motion Override)]
    /// 유니티 엔진이 애니메이션의 움직임을 실제 트랜스폼에 적용하기 직전에 가로채는 매우 강력한 콜백 함수입니다.
    /// </summary>
    public override void OnAnimatorMoveCallback()
    {
        if (anim == null || rb == null) return;

        // 점프 중이 아닐 때는 물리 엔진(Rigidbody)에게 100% 통제권을 넘김
        if (!isJumping)
        {
            rb.MovePosition(rb.position);
            return;
        }

        // [점프 궤적 수학적 덮어쓰기]
        // 1. X축(수평 이동): 애니메이션의 진행도 곡선("JumpProgress" 파라미터)을 읽어와 선형 보간(Lerp) 비율로 사용합니다.
        // 이를 통해 애니메이터가 커브 곡선으로 설계한 템포(초반엔 느리게, 찍을 땐 확 빠르게)대로 X축을 이동시킵니다.
        currentJumpProgress = anim.GetFloat("JumpProgress");
        float newX = Mathf.Lerp(jumpStartPos.x, jumpTargetPos.x, currentJumpProgress);

        // 2. Y축(수직 이동): 보스가 뛰어오르는 높이는 애니메이션 클립(.anim)이 가진 Y축 변화량(deltaPosition.y)을 순수하게 더해줍니다.
        float newY = transform.position.y + anim.deltaPosition.y;

        // 3. Z축(깊이): 횡스크롤이므로 0으로 고정
        float curZ = 0f;

        // 물리 엔진을 완전히 무시하고 보스의 좌표를 멱살 잡아 강제로 꽂아 넣습니다.
        transform.position = new Vector3(newX, newY, curZ);
    }
    #endregion
}