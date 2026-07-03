using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

public class Cinderella_Patterns : MonoBehaviour, IInitializable, IBossLogics
{
    public int Priority => (int)InitOrder.Boss + 1;

    #region 인스펙터
    [Header("Animator")]
    [SerializeField] private Animator anim;

    [Header("VFX")]
    [SerializeField] private GameObject telegraph;
    private RingDrawer telegraphDrawer;
    
    [Header("위치")]
    [Tooltip("보스 스폰 위치")][SerializeField] private Transform spawnPos;
    [Tooltip("플레이어 위치")][SerializeField] private Transform playerPos;
    [Tooltip("이동 가능한 바닥 감지")][SerializeField] private BoxCollider ground;

    [Header("히트스탑")]
    [Tooltip("패링시 히트스탑 프레임")][SerializeField] private int HitStopFrame;
    [Tooltip("히트스탑시 카메라 흔들림 강도")][SerializeField] private float cameraShakeIntensity;
    
    [Header("Idle 상태")]
    [SerializeField] private float idleDurationTime;//idle 정지상태 지속시간
    [Tooltip("이동속도")][Min(0)] [SerializeField] private float move_idleSpeed;
    [Tooltip("목표 좌표 근처 이동 가능한 범위")][Min(0)] [SerializeField] private float move_idleRange;  //이동가능 범위
    [Min(3)] [SerializeField] private float move_idlePos;    //플레이어 기준 이동범위 중심
    
    [Header("Enranged 상태")]
    [SerializeField] private float durationEnranged; //지속시간
    [SerializeField] private float enrangedAtkSpeed_Mul; //공격속도
    
    [Header("AttackA 상태")]
    [SerializeField] private float A_ChaseSpeed;
    [SerializeField] private Vector3 A_ChasePos;
    [SerializeField] private float A_telegraphTime;
    [Tooltip("공격 발동 시간")][SerializeField] private float A_attackDuration;
    [Tooltip("후딜 시간")][SerializeField] private float A_postAtkDelay;
    [Tooltip("패링 시 보스 경직 시간")][SerializeField] private float A_parryStunDuration;
    
    [Header("AttackB 상태")]
    [SerializeField] private float B_ChaseSpeed;
    [SerializeField] private Vector3 B_ChasePos;
    [SerializeField] private float B_telegraphTime;
    [Tooltip("후딜 시간")][SerializeField] private float B_postAtkDelay;
    
    [Header("AttackC 상태")]
    [SerializeField] private float C_ChaseSpeed;
    [SerializeField] private Vector3 C_ChasePos;
    [Tooltip("후딜 시간")][SerializeField] private float C_postAtkDelay;
    
    [Header("Ultimate 상태")]
    [SerializeField] private float UltimateChaseSpeed;
    [SerializeField] private Vector3 UltimateChasePos;
    [SerializeField] private float ULTI_telegraphTime1;
    [SerializeField] private float ULTI_telegraphTime2;
    [SerializeField] private float ULTI_telegraphTime3;
    
    [Header("Groggy 상태")]
    [SerializeField] private float groggyDuration; //그로기 지속시간
    #endregion

    #region 변수
    //공통 사용
    public float Distance => playerPos.position.x - transform.position.x;
    public bool StateDone { get; private set; } //공격 BT 종료 여부
    public bool IsParryed => isParryed;
    public bool IsEnranged => isEnranged;
    public bool IsPhysicsOverridden => isJumping;

    public BossStatus bossStatus { get; private set; }
    private Transform bossSkin;
    private Rigidbody rb;
    private Node kickAttack;
    private Node spinShardAttack;
    private Node jumpSlamAttack;
    private Node UltimateAttack;

    private float curTime_Anim = 0f;
    private float cachedLocomotionSpeed = -999f; //이전 프레임에 주입했던 파라미터 값
    private AnimatorStateInfo animState;
    private AttackType attackType;  //현재 공격 타입
    private AttackType beforeType = AttackType.C;  //이전 공격 타입
    private Facing curFacing = Facing.Left; //보스가 현재 바라보는 방향
    private readonly float[] rotValue = //Animation Y축 각도 설정값
    {
        228, -228, //Idle
        -90, 90, 0,0, -90,90, //Attack
        -90,90,-90,90,-90,90, //Ultimate
        -90, 90, //Chase
        228, -228, //Parry
        -90, 90, //Groggy
        -90, 90 //걷기
    };

    //Idle
    private Vector3 RandomPos;
    private float curTime_Idle = 0f;
    private bool hasArrived = false; //목적지 도착여부

    //사전신호
    private bool telegraphExcuted = false;

    //공격
    private bool isParryed = false;     //패링 되었는지 여부

    //공격A 타입
    private bool chaseDone = false;    //추격 실행 여부
    private bool attackDone = false;    //공격 실행 여부

    //공격B 타입
    private bool isParryCanceled = false; //패링으로 캔슬된 공격인가?

    //공격C 타입
    private bool isJumping = false;
    private Vector3 jumpStartPos;
    private Vector3 jumpTargetPos;
    private float currentJumpProgress = 0f; //OnAnimatorMove와 공유할 데이터 통로

    //Ultimate
    private int parryCount = 0;
    private int comboStep = 0;

    //Groggy
    private bool isGroggyAnimDone = false;

    //Enranged
    private bool isEnranged = false;    //현재 격노 상태인지
    private float curEnrangedTime = 0;  //현재 격노 타이머
    private float curEnrangedAtkSpeed = 1; //현재 격노 속도

    //Move
    private float x, y, z; //Move에 사용될 속도 저장
    private float groundXMin; //이동 가능지역 최솟값 x
    private float groundXMax; //이동 가능지역 최대값 x
    #endregion

    public void Init()
    {
        bossStatus = ServiceLocator_Y.Get<BossStatus>();
        bossSkin = transform.GetChild(0).transform;
        rb = GetComponent<Rigidbody>();
        telegraphDrawer = telegraph.GetComponent<RingDrawer>(); //사전신호
        telegraphDrawer.Init();
        Init_BT();

        //이동가능 x좌표
        groundXMin = ground.bounds.min.x;
        groundXMax = ground.bounds.max.x;
        if (move_idleRange >= move_idlePos) move_idleRange = move_idlePos - 0.5f;
    }

    private void OnEnable()
    {
        EventBus<ParryKeyDown>.action += ParryKeyDown;
    }
    private void OnDisable()
    {
        EventBus<ParryKeyDown>.action -= ParryKeyDown;
    }

    public void Init_BT()
    {
        kickAttack = new Selector(new List<Node>
        {
            //패링 시 주춤
            new StatefulSequence(new List<Node>
            {
                new ConditionLeaf(() => isParryed),
                new Leaf(() =>
                {
                    EventBus<CameraShakeEvent>.Publish(new CameraShakeEvent(cameraShakeIntensity));
                    EventBus<HitStopEvent>.Publish(new HitStopEvent(HitStopFrame));
                    return NodeState.Success;
                }),
                new Leaf(() => PlayAnim_Speed((int)Animation.Parry, A_parryStunDuration)),
                new Leaf(() => 
                {
                    Parryed();
                    SetStateDone(true);
                    return NodeState.Success; 
                })
            }),
            //공격 로직
            new Sequence(new List<Node>
            {
                //추격 실렉터
                new Selector(new List<Node>
                {
                    new ConditionLeaf(() => chaseDone), //추격이 끝났는가? -> 다음 시퀀스
                    new Leaf(() => Chase(A_ChasePos, A_ChaseSpeed, (int)Animation.Chase)) //해당 위치까지 이동
                }),
                //사전신호
                new Selector(new List<Node>
                {
                    new ConditionLeaf(() => telegraphExcuted), //사전신호 발생했는가?
                    new Leaf(() =>
                    {
                        telegraph.SetActive(true);
                        telegraphDrawer.PlaySignal(GetAdjustedTelegraphTime(A_telegraphTime));
                        telegraphExcuted = true;
                        return NodeState.Success;
                    }) //사전신호 발생
                }),
                //공격 실렉터
                new Selector(new List<Node>
                {
                    new ConditionLeaf(() => attackDone), //공격 애님 끝남?
                    new StatefulSequence(new List<Node>
                    {
                        new Leaf(() => { UpdateFacing(); return NodeState.Success; }),
                        new Leaf(() => PlayAnim_Speed((int)Animation.AttackA, A_attackDuration)), //공격 애님 실행
                        new Leaf(() => {
                            //Debug.Log("여기서 오류남!");
                            attackDone = true;
                            return NodeState.Success;
                        })
                    })
                }),
                //후딜 시퀀스
                new Sequence(new List<Node>
                {
                    new Leaf(() =>
                    {
                        UpdateFacing();
                        return PlayAnim_Time((int)Animation.Idle, A_postAtkDelay); //Idle 애님 실행
                    }),
                    new Leaf(() => SetStateDone(true))
                })
            })
        });

        spinShardAttack = new Selector(new List<Node>
        {
            //패링 시 주춤
            new StatefulSequence(new List<Node>
            {
                new ConditionLeaf(() => isParryed),
                new Leaf(() => 
                {
                    EventBus<CameraShakeEvent>.Publish(new CameraShakeEvent(cameraShakeIntensity));
                    EventBus<HitStopEvent>.Publish(new HitStopEvent(HitStopFrame));
                    return NodeState.Success;
                }),
                new Leaf(() => PlayAnim_Speed((int)Animation.Parry, 0f)),
                new Leaf(() =>
                {
                    Parryed();
                    
                    bossStatus.TakeDamage(30); //패링시 파편반사로 인한 데미지
                    SetStateDone(true);
                    return NodeState.Success;
                })
            }),
            //공격 로직
            new Sequence(new List<Node>
            {
                //추격 실렉터
                new Selector(new List<Node>
                {
                    new ConditionLeaf(() => chaseDone), //추격이 끝났는가? -> 다음 시퀀스
                    new Leaf(() => Chase(B_ChasePos, B_ChaseSpeed, (int)Animation.Chase)) //해당 위치까지 이동
                }),
                //사전신호
                new Selector(new List<Node>
                {
                    new ConditionLeaf(() => telegraphExcuted), //사전신호 발생했는가?
                    new Leaf(() =>
                    {
                        telegraph.SetActive(true);
                        telegraphDrawer.PlaySignal(GetAdjustedTelegraphTime(B_telegraphTime));
                        telegraphExcuted = true;
                        return NodeState.Success;
                    }) //사전신호 발생
                }),
                //공격 실렉터
                new Selector(new List<Node>
                {
                    new ConditionLeaf(() => attackDone), //공격 애님 끝남?
                    new StatefulSequence(new List<Node>
                    {
                        new Leaf(() => { UpdateFacing(); return NodeState.Success; }),
                        new Leaf(() => PlayAnim_Speed((int)Animation.AttackB, 0f)), //공격 애님 실행
                        new Leaf(() => {
                            if(!isParryCanceled)
                                EventBus<OnShardHitBoxEvent>.Publish(new OnShardHitBoxEvent(transform)); //장판 깔아주기
                            attackDone = true;
                            return NodeState.Success;
                        })
                    })
                }),
                //후딜 시퀀스
                new Sequence(new List<Node>
                {
                    new Leaf(() =>
                    {
                        UpdateFacing();
                        return PlayAnim_Time((int)Animation.Idle, B_postAtkDelay); //Idle 애님 실행
                    }),
                    new Leaf(() => SetStateDone(true))
                })
            })
        });

        jumpSlamAttack = new Selector(new List<Node>
        {
            //공격 로직
            new Sequence(new List<Node>
            {
                //추격 실렉터
                new Selector(new List<Node>
                {
                    new ConditionLeaf(() => chaseDone), //추격이 끝났는가? -> 다음 시퀀스
                    new Leaf(() => Chase(C_ChasePos, C_ChaseSpeed, (int)Animation.Chase)) //해당 위치까지 이동
                }),
                //사전신호
                new Sequence(new List<Node>
                {
                    //new ConditionLeaf(), //사전신호 발생했는가?
                    //new Leaf() //사전신호 발생
                }),
                //공격 실렉터
                new Selector(new List<Node>
                {
                    new ConditionLeaf(() => attackDone), //공격 애님 끝남?
                    new StatefulSequence(new List<Node>
                    {
                        new Leaf(() => DynamicJumpSlam(playerPos.position, (int)Animation.AttackC)),
                        new Leaf(() =>
                        {
                            attackDone = true;
                            return NodeState.Success;
                        })
                    })
                }),
                //후딜 시퀀스
                new Sequence(new List<Node>
                {
                    new Leaf(() =>
                    {
                        UpdateFacing();
                        return PlayAnim_Time((int)Animation.Idle, C_postAtkDelay); //Idle 애님 실행
                    }),
                    new Leaf(() => SetStateDone(true))
                })
            })
        });

        UltimateAttack = new Selector(new List<Node>
        {
            //패링 시 주춤
            new StatefulSequence(new List<Node>
            {
                new ConditionLeaf(() => isParryed),
                new Leaf(() =>
                {
                    EventBus<CameraShakeEvent>.Publish(new CameraShakeEvent(cameraShakeIntensity));
                    EventBus<HitStopEvent>.Publish(new HitStopEvent(HitStopFrame));
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
            //공격 로직
            new Sequence(new List<Node>
            {
                //공격 타입 설정
                new Leaf(() => {attackType = AttackType.D; return NodeState.Success; }),
                //추격
                new Selector(new List<Node>
                {
                    new ConditionLeaf(() => chaseDone),
                    new Leaf(() => Chase(UltimateChasePos, UltimateChaseSpeed, (int)Animation.Chase))
                }),
                //사전신호
                new Selector(new List<Node>
                {
                    new ConditionLeaf(() => telegraphExcuted), //사전신호 발생했는가?
                    new Leaf(() =>
                    {
                        telegraph.SetActive(true);
                        float time;
                        if(comboStep == 0) time = ULTI_telegraphTime1;
                        else if(comboStep == 1) time = ULTI_telegraphTime2;
                        else time = ULTI_telegraphTime3;
                        telegraphDrawer.PlaySignal(GetAdjustedTelegraphTime(time));
                        telegraphExcuted = true;
                        return NodeState.Success;
                    }) //사전신호 발생
                }),
                //공격
                new Selector(new List<Node>
                {
                    new ConditionLeaf(() => attackDone),
                    new StatefulSequence(new List<Node>
                    {
                        new Leaf(() => { UpdateFacing(); return NodeState.Success; }),
                        new Selector(new List<Node> //첫번째 공격
                        {
                            new ConditionLeaf(() => comboStep > 0),
                            new Sequence(new List<Node>
                            {
                                new Leaf(() => PlayAnim_Speed((int)Animation.Ultimate1, 0f)), //나중에 애니메이션 변경
                                new Leaf(() => { EventBus<AttackFinishEvent>.Publish(default); return NodeState.Success; }), //중복공격 초기화
                                new Leaf(() => 
                                { 
                                    comboStep++;
                                    telegraphExcuted = false;
                                    return NodeState.Success; 
                                })
                            }),
                        }),
                        new Leaf(() => { UpdateFacing(); return NodeState.Success; }),
                        new Selector(new List<Node> //두번째 공격
                        {
                            new ConditionLeaf(() => comboStep > 1),
                            new Sequence(new List<Node>
                            {
                                new Leaf(() => PlayAnim_Speed((int)Animation.Ultimate2, 0f)), //나중에 애니메이션 변경
                                new Leaf(() => { EventBus<AttackFinishEvent>.Publish(default); return NodeState.Success; }), //중복공격 초기화
                                new Leaf(() =>
                                {
                                    comboStep++;
                                    telegraphExcuted = false;
                                    return NodeState.Success;
                                })
                            }),
                        }),
                        new Leaf(() => { UpdateFacing(); return NodeState.Success; }),
                        new Selector(new List<Node> //세번째 공격
                        {
                            new ConditionLeaf(() => comboStep > 2),
                            new Sequence(new List<Node>
                            {
                                new Leaf(() => PlayAnim_Speed((int)Animation.Ultimate3, 0f)), //나중에 애니메이션 변경
                                new Leaf(() => { EventBus<AttackFinishEvent>.Publish(default); return NodeState.Success; }), //중복공격 초기화
                                new Leaf(() =>
                                {
                                    comboStep++;
                                    telegraphExcuted = false;
                                    return NodeState.Success;
                                })
                            }),
                        }),
                        new Leaf(() =>
                        {
                            Debug.Log("궁극기 공격 종료");
                            attackDone = true;
                            StateDone = true;
                            return NodeState.Success;
                        })
                    })
                })
            })
            //FSM 상태에서 Groggy로 전환
        });
    }

    #region CommonLogic
    private void ParryKeyDown(ParryKeyDown data) { isParryed = true; } //패링 여부 확인
    private void Parryed() //패링되었음(패링가능, 콜라이더 토글 끄기)
    {
        telegraphExcuted = false; //사전신호 초기화
        isParryed = false;
        isParryCanceled = true;
        EventBus<CanParryEvent>.Publish(new CanParryEvent(false));
        EventBus<ColliderToggleEvent>.Publish(new ColliderToggleEvent(attackType, false));
    }
    public bool IsAttacking() //애니메이터 파라미터 읽어 현재 공격 상태인지 식별
    {
        int currentAnim = anim.GetInteger("Boss");
        return currentAnim == (int)Animation.AttackA ||
            currentAnim == (int)Animation.AttackB ||
            currentAnim == (int)Animation.AttackC ||
            currentAnim == (int)Animation.Ultimate1 ||
            currentAnim == (int)Animation.Ultimate2 ||
            currentAnim == (int)Animation.Ultimate3;
    }
    private void Move(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }
    public void ExcuteMove()
    {
        rb.linearVelocity = new Vector3(x, y, z);
        //Debug.Log($"{isParryed}");
    }
    public void UpdateFacing() //보스가 플레이어 바라보는 방향 갱신
    {
        Facing newFacing = (Distance < 0) ? Facing.Left : Facing.Right;
        if (curFacing != newFacing) //방향이 바뀌었을 때 1회 발생
        {
            curFacing = newFacing;
            EventBus<BossFacingChangeEvent>.Publish(new BossFacingChangeEvent(curFacing));
        }
        //Debug.Log($"UpdateFacing: {curFacing}");
    }
    public void SyncFacingWithAnim(int targetAnim) //방향과 애님 동기화
    {
        //Debug.Log($"SyncFacingWithAnim: {targetAnim}");
        bool shouldMirror = (curFacing == Facing.Right);
        anim.SetBool("isMirrored", shouldMirror);
        anim.SetInteger("Boss", targetAnim);
        int angleIndex = (curFacing == Facing.Left) ? (targetAnim * 2) : (targetAnim * 2 + 1);
        bossSkin.rotation = Quaternion.Euler(0f, rotValue[angleIndex], 0f);
    }
    //애니메이션 준비 단계
    public bool IsAnimationReady(int targetAnim)
    {
        int curAnim = anim.GetInteger("Boss");
        bool curMirror = anim.GetBool("isMirrored");
        bool targetMirror = (curFacing == Facing.Right);
        if (curAnim != targetAnim) //액션 자체 변화, 애니메이션 전환
        {
            //if (curAnim / 2 != targetAnim / 2) 
            curTime_Anim = 0f;
            SyncFacingWithAnim(targetAnim);
            return false; //방어코드(프레임 갱신)
        }
        if(curMirror != targetMirror) //방향만 변화, 애니메이션 전환
        {
            SyncFacingWithAnim(targetAnim);
            return false;
        }
        if (anim.IsInTransition(0)) return false; //방어코드(애님 전환 중)
        return true;
    }
    //애니메이션 재생(속도 조절해서 시간을 맞춤)
    public NodeState PlayAnim_Speed(int num, float targetSeconds) 
    {
        if (!IsAnimationReady(num)) return NodeState.Running;
        //Debug.Log($"PlayAnim {num} 재생중");
        //원하는 시간만큼 애님 재생
        animState = anim.GetCurrentAnimatorStateInfo(0);
        //공격애니메이션만 속도 조절
        if (isEnranged && (num == (int)Animation.AttackA || num == (int)Animation.AttackB || num == (int)Animation.AttackC))
        {
            anim.speed = (targetSeconds == 0) ?
                (animState.length / animState.length * curEnrangedAtkSpeed)
                : (animState.length / targetSeconds * curEnrangedAtkSpeed);
        }
        else
        {
            anim.speed = (targetSeconds == 0) ? 
                (animState.length / animState.length) : animState.length / targetSeconds;
        }
        if (animState.normalizedTime < 0.95f) return NodeState.Running;
        else { anim.speed = 1.0f; return NodeState.Success; }
    }
    //애니메이션 재생(일정시간이 지나면 다음으로 넘어감)
    public NodeState PlayAnim_Time(int num, float targetSeconds) 
    {
        if (!IsAnimationReady(num)) return NodeState.Running;
        curTime_Anim += Time.deltaTime;
        //원하는 시간만큼 애님 재생
        animState = anim.GetCurrentAnimatorStateInfo(0);
        if (curTime_Anim < targetSeconds) return NodeState.Running;
        else { anim.speed = 1.0f; return NodeState.Success; }
    }
    public NodeState SetStateDone(bool set)
    {
        if(StateDone != set) StateDone = set;
        //Debug.Log($"SetStateDone {set}");
        return NodeState.Success;
    }
    public bool GetStateDone() { return StateDone; } //상태 종료 확인
    #endregion

    #region Spawn
    public void Spawn() //SpawningState
    {
        transform.position = spawnPos.position;
    }
    #endregion

    #region Idle
    public void InitCurTime_Idle() { curTime_Idle = 0f; hasArrived = false; }  //시간 및 도착 플래그 초기화
    public void SetRandomPos() //플레이어 기준 좌우 좌표 지정
    {
        float element = move_idlePos + Random.Range(-move_idleRange, move_idleRange);
        //플레이어가 보스 오른쪽에 위치
        if (Math.Sign(Distance) <= 0) RandomPos = new Vector3(playerPos.position.x + element, 0, 0);
        //플레이어가 보스 왼쪽에 위치
        else RandomPos = new Vector3(playerPos.position.x - element, 0, 0);
        //이동가능 범위 밖으로 벗어나지 않도록 클램핑
        RandomPos.x = Mathf.Clamp(RandomPos.x, groundXMin, groundXMax); 
    }
    //캡슐화: 애니메이션과 방향 계산만 전담하는 헬퍼 메서드
    private void SyncLocomotionAnim(float moveDirX)
    {
        //이번 프레임의 목표값 계산
        int targetAnim;
        float targetSpeed;

        if(moveDirX == 0f)
        {
            targetAnim = (int)Animation.Idle;
            targetSpeed = 0f;
        }
        else
        {
            targetAnim = (int)Animation.Walking;
            float facingDir = (curFacing == Facing.Right) ? 1f : -1f; //시선(우측1, 좌측-1)
            targetSpeed = moveDirX * facingDir; //전후진 판변(이동방향*시선방향)
        }

        //목표값이 이전 프레임과 동일하다면 렌더링 호출 생략
        if (Mathf.Abs(cachedLocomotionSpeed - targetSpeed) < 0.01f) return;

        //값이 변했을 때 1회 호출
        PlayAnim_Time(targetAnim, idleDurationTime);
        anim.SetFloat("MoveForward", targetSpeed);
        cachedLocomotionSpeed = targetSpeed;
    }
    public void IdleMove() //보스 기준
    {
        UpdateFacing();
        //walking
        if (!hasArrived)
        {
            //목적지까지 절대거리 계산
            float distToTarget = Mathf.Abs(RandomPos.x - transform.position.x);
            //도착 판정 (오차범위 이내 or 플레이어 방향, 목표방향 같고)
            if (distToTarget <= 0.5f || (Math.Sign(Distance) == Math.Sign(RandomPos.x - transform.position.x) && distToTarget > Math.Abs(Distance)) )
            {
                Debug.Log("걷기 종료");
                hasArrived = true;      //상태락 걸기
                Move(0, 0, 0);          //물리적 정지
                SyncLocomotionAnim(0f); //시각적 정지
            }
            else //아직 도착 안했을 때
            {
                float moveDirX = (transform.position.x < RandomPos.x) ? 1f : -1f;
                Move(moveDirX * move_idleSpeed, 0, 0);
                SyncLocomotionAnim(moveDirX);
            }
        }
        //Idle
        else
        {
            curTime_Idle += Time.deltaTime;
            if(curTime_Idle > idleDurationTime)
            {
                SetStateDone(true);
            }
        }
    }
    #endregion

    #region Attack
    public AttackType GetAttackType() { return attackType; }
    public Node GetAttackBT()
    {
        do
        {
            attackType = (AttackType)Random.Range(0, 3);
        }
        while (attackType == beforeType);
        beforeType = attackType;
        //attackType = AttackType.B;
        //Debug.Log($"{attackType} 공격 실행");
        switch (attackType)
        {
            case AttackType.A:
                return kickAttack;
            case AttackType.B:
                return spinShardAttack;
            case AttackType.C:
                return jumpSlamAttack;
        }
        return default;
    }

    private NodeState Chase(Vector3 pos, float speed, int animNum)
    {
        //애니메이션 적용
        UpdateFacing(); //실시간 방향 갱신
        //애니메이션 L,R 동기화
        if (!IsAnimationReady(animNum))
        {
            Move(0, 0, 0);
            return NodeState.Running;
        }

        //물리 이동 및 거리 계산
        if (Math.Abs(Distance) > pos.x)
        {
            //Debug.Log("오른쪽 호출");
            Move(Math.Sign(Distance) * speed, 0, 0);
            return NodeState.Running;
        }
        else
        {
            //Debug.Log("왼쪽 호출");
            Move(0, 0, 0);
            chaseDone = true;
            return NodeState.Success;
        }
    }

    public void LogicInit()
    {
        chaseDone = false;
        telegraphExcuted = false;
        attackDone = false;
        isParryCanceled = false;
        parryCount = 0;
        comboStep = 0;

        UltimateAttack?.Reset();
        isGroggyAnimDone = false; //다음 궁극기에서 사용할 수 있도록 다시 초기화
        isJumping = false;
    }
    #endregion

    #region KickAttack

    #endregion

    #region SpinShardAttack
    #endregion

    #region JumpSlamAttack
    private NodeState DynamicJumpSlam(Vector3 targetPos,  int animNum)
    {
        //단 1회 실행되는 스냅샷
        if (!isJumping)
        {
            UpdateFacing();
            SyncFacingWithAnim(animNum);

            jumpStartPos = transform.position;  //도약지점 저장
            jumpTargetPos = targetPos;          //타겟 지점(플레이어) 저장
            isJumping = true;
            currentJumpProgress = 0f;

            return NodeState.Running;
        }
        //애니메이션 섞임 대기
        if (anim.IsInTransition(0)) return NodeState.Running;
        //종료 판정
        animState = anim.GetCurrentAnimatorStateInfo(0);
        //진행도 0.5일때 파동공격 콜라이더 실행
        if (animState.normalizedTime >= 0.3f)
            attackType = AttackType.C_2;
        //진행도가 1.0에 근접했다면
        if(animState.normalizedTime >= 0.95f)
        {
            isJumping = false;
            attackType = AttackType.C;
            return NodeState.Success;
        }
        return NodeState.Running; //아직 공중에 있음
    }
    //유니티 물리/애니메이션 렌더링 직전에 자동 호출되는 콜백(강력한 덥어쓰기 권한)
    public void OnAnimatorMoveCallback()
    {
        if (anim == null || rb == null) return;
        //평상시
        if (!isJumping)
        {
            rb.MovePosition(rb.position);
            return;
        }
        //현재 프레임 진행도 저장
        currentJumpProgress = anim.GetFloat("JumpProgress");
        //점프 공격 중
        //x축(거리): 애니메이션의 x축 루트모션을 무시하고, 코드의 절대좌표로 덮어씌움
        float newX = Mathf.Lerp(jumpStartPos.x, jumpTargetPos.x, currentJumpProgress);
        //y축(높이): 애니메이션 클립이 제공하는 프레임당 높이 변화량만 선별적으로 가져와 더함
        float newY = transform.position.y + anim.deltaPosition.y;
        //z축(깊이): 현재 깊이 유지
        float curZ = 0f;
        //메모리 덮어쓰기
        transform.position = new Vector3(newX, newY, curZ);
    }
    #endregion

    #region Ultimate
    public Node GetUltimateBT() { return UltimateAttack; }
    public bool CanTransitionToGroggy() { return parryCount >= 3; }
    #endregion

    #region Groggy
    public NodeState PlayAnimGroggy_Time(int num)
    {
        if (!isGroggyAnimDone)
        {
            Debug.Log("그로기 애님 진행중");
            if (PlayAnim_Time(num, groggyDuration) == NodeState.Success)
            {
                isGroggyAnimDone = true;
                bossStatus.SetGroggyDamageMultiplierActive(false); //그로기 피격 배율 복귀
                return NodeState.Running;
            }
        }
        //그로기 끝나고 바로 공격해서 이상하다는 피드백 수용
        else if (PlayAnim_Time((int)Animation.Idle, 1) == NodeState.Success)
        {
            //Debug.Log("Idle 애님 success");
            isEnranged = true;
            curEnrangedAtkSpeed = enrangedAtkSpeed_Mul;
            return NodeState.Success;
        }
        return NodeState.Running;
    }
    //Enranged
    //격노 상태에서 공속 배율에 맞추어 사전신호 시간 계산
    private float GetAdjustedTelegraphTime(float baseTime) { return baseTime / curEnrangedAtkSpeed; }
    public void EnrangedTimer()
    {
        curEnrangedTime += Time.deltaTime;
        if(curEnrangedTime >= durationEnranged)
        {
            isEnranged = false;
            curEnrangedTime = 0f;
            curEnrangedAtkSpeed = 1f;
            Debug.Log("격노 종료");
        }
    }
    #endregion
}
