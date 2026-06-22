using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

public class Cinderella_Patterns : MonoBehaviour, IInitializable, IBossLogics
{
    public int Priority => (int)InitOrder.Boss + 1;

    [Header("Animator")]
    [SerializeField] private Animator anim;
    [Header("위치")]
    [SerializeField] private Transform spawnPos;
    [SerializeField] private Transform playerPos;
    [Header("Idle 상태")]
    [SerializeField] private float idleDurationTime;//idle 정지상태 지속시간
    [Min(0)] [SerializeField] private float move_idleSpeed;
    [Min(0)] [SerializeField] private float move_idleRange;  //이동가능 범위
    [Min(3)] [SerializeField] private float move_idlePos;    //플레이어 기준 이동범위 중심
    [Header("Enranged 상태")]
    [SerializeField] private float durationEnranged; //지속시간
    [SerializeField] private float enrangedAtkSpeed_Mul; //공격속도
    [Header("AttackA 상태")]
    [SerializeField] private float A_ChaseSpeed;
    [SerializeField] private Vector3 A_ChasePos;
    [SerializeField] private float A_postAtkDelay;
    [Header("AttackB 상태")]
    [SerializeField] private float B_ChaseSpeed;
    [SerializeField] private Vector3 B_ChasePos;
    [SerializeField] private float B_postAtkDelay;
    [Header("AttackC 상태")]
    [SerializeField] private float C_ChaseSpeed;
    [SerializeField] private Vector3 C_ChasePos;
    [SerializeField] private float C_postAtkDelay;
    [Header("Ultimate 상태")]
    [SerializeField] private float UltimateChaseSpeed;
    [SerializeField] private Vector3 UltimateChasePos;
    [Header("Groggy 상태")]
    [SerializeField] private float groggyDuration; //그로기 지속시간

    //공통 사용
    public float Distance => playerPos.position.x - transform.position.x;
    public bool StateDone { get; private set; } //공격 BT 종료 여부
    public bool IsParryed => isParryed;
    public bool IsEnranged => isEnranged;

    private Transform bossSkin;
    private Rigidbody rb;
    private AnimatorStateInfo animState;
    private float curTime_Anim = 0f;
    private float curTime_Idle = 0f;
    private bool isParryed = false;     //패링 되었는지 여부

    private Node kickAttack;
    private Node spinShardAttack;
    private Node jumpSlamAttack;
    private Node UltimateAttack;
    private AttackType attackType;  //현재 공격 타입
    private AttackType beforeType = AttackType.C;  //이전 공격 타입
    private Facing curFacing = Facing.Left; //보스가 현재 바라보는 방향
    private readonly float[] rotValue = //Animation Y축 각도 설정값
    {
        228, -228, //Idle
        -90, 90, 0,0,0,0, //Attack
        -90,90,-90,90,-90,90, //Ultimate
        -90, 90, //Chase
        228, -228, //Parry
        -90, 90 //Groggy
    };

    //Idle
    private Vector3 RandomPos;

    //공격A 타입
    private bool chaseDone = false;    //추격 실행 여부
    private bool telegraphExcuted = false;
    private bool attackDone = false;    //공격 실행 여부

    //공격B 타입
    private bool isParryCanceled = false; //패링으로 캔슬된 공격인가?

    //Ultimate
    private int parryCount = 0;
    private int comboStep = 0;

    //Enranged
    private bool isEnranged = false;    //현재 격노 상태인지
    private float curEnrangedTime = 0;  //현재 격노 타이머
    private float curEnrangedAtkSpeed = 1; //현재 격노 속도


    private float x, y, z; //Move에 사용될 속도 저장

    public void Init()
    {
        bossSkin = transform.GetChild(0).transform;
        rb = GetComponent<Rigidbody>();
        Init_BT();
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
                new Leaf(() => PlayAnim_Speed((int)Animation.Parry, 0f)),
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
                        new Leaf(() => { UpdateFacing(); return NodeState.Success; }),
                        new Leaf(() => PlayAnim_Speed((int)Animation.AttackA, 0f)), //공격 애님 실행
                        new Leaf(() => {
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
                new Leaf(() => PlayAnim_Speed((int)Animation.Parry, 0f)),
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
                        new Leaf(() => { UpdateFacing(); return NodeState.Success; }),
                        new Leaf(() => PlayAnim_Speed((int)Animation.AttackB, 0f)), //공격 애님 실행
                        new Leaf(() => {
                            if(!isParryCanceled)
                                EventBus<OnShardHitBox>.Publish(new OnShardHitBox(transform)); //장판 깔아주기
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

        jumpSlamAttack = new Selector(new List<Node>
        {
            //패링 시 주춤
            new StatefulSequence(new List<Node>
            {
                new ConditionLeaf(() => isParryed),
                new Leaf(() => PlayAnim_Speed((int)Animation.Parry, 0f)),
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
                    new ConditionLeaf(() => chaseDone || Math.Abs(Distance) < 10), //추격이 끝났는가? -> 다음 시퀀스
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
                        new Leaf(() => { UpdateFacing(); return NodeState.Success; }),
                        new Leaf(() => PlayAnim_Speed((int)Animation.AttackB, 0f)), //공격 애님 실행
                        new Leaf(() => {
                            if(!isParryCanceled)
                                EventBus<OnShardHitBox>.Publish(new OnShardHitBox(transform)); //장판 깔아주기
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

        UltimateAttack = new Selector(new List<Node>
        {
            //패링 시 주춤
            new StatefulSequence(new List<Node>
            {
                new ConditionLeaf(() => isParryed),
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
                //추격
                new Selector(new List<Node>
                {
                    new ConditionLeaf(() => chaseDone),
                    new Leaf(() => Chase(UltimateChasePos, UltimateChaseSpeed, (int)Animation.Chase))
                }),
                //사전신호
                new Sequence(new List<Node>
                {

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
                                new Leaf(() => { comboStep++; return NodeState.Success; })
                            }),
                        }),
                        new Leaf(() => { UpdateFacing(); return NodeState.Success; }),
                        new Selector(new List<Node> //두번째 공격
                        {
                            new ConditionLeaf(() => comboStep > 1),
                            new Sequence(new List<Node>
                            {
                                new Leaf(() => PlayAnim_Speed((int)Animation.Ultimate2, 0f)), //나중에 애니메이션 변경
                                new Leaf(() => { comboStep++; return NodeState.Success; }),
                            }),
                        }),
                        new Leaf(() => { UpdateFacing(); return NodeState.Success; }),
                        new Selector(new List<Node> //세번째 공격
                        {
                            new ConditionLeaf(() => comboStep > 2),
                            new Sequence(new List<Node>
                            {
                                new Leaf(() => PlayAnim_Speed((int)Animation.Ultimate3, 0f)), //나중에 애니메이션 변경
                                new Leaf(() => { comboStep++; return NodeState.Success; }),
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
    public void InitCurTime_Idle() { curTime_Idle = 0f; }  //시간 초기화
    public void SetRandomPos() //플레이어 기준 좌우 좌표 지정
    {
        float element = move_idlePos + Random.Range(-move_idleRange, move_idleRange);
        if (Math.Sign(Distance) <= 0) //플레이어가 보스 오른쪽에 위치
        {
            RandomPos = new Vector3(playerPos.position.x + element, 0, 0);
            //Debug.Log($"플레이어 - 보스 = {Distance}");
            //Debug.Log($"{RandomPos.x}, {RandomPos.y}, {RandomPos.z}");
        }
        else    //플레이어가 보스 왼쪽에 위치
            RandomPos = new Vector3(playerPos.position.x - element, 0, 0);
    }
    public void IdleMove() //보스 기준
    {
        UpdateFacing();
        PlayAnim_Time((int)Animation.Idle, 1f);
        if (Math.Sign(Distance) == Math.Sign(RandomPos.x - transform.position.x))
            { Move(0, 0, 0); curTime_Idle += Time.deltaTime; }
        else if (transform.position.x < RandomPos.x -0.5)    //지정좌표 왼쪽에 있을 때
            Move(move_idleSpeed, 0, 0);
        else if (transform.position.x > RandomPos.x +0.5)   //지정좌표 오른쪽에 있을 때
            Move(-move_idleSpeed, 0, 0);
        else { Move(0, 0, 0); curTime_Idle += Time.deltaTime; }
        if (curTime_Idle > idleDurationTime) SetStateDone(true); //지속시간 지나면 상태전이
    }
    #endregion

    #region Attack
    public AttackType GetAttackType() { return attackType; }
    public Node GetAttackBT()
    {
        while (attackType == beforeType) attackType = (AttackType)Random.Range(0, 2);
        beforeType = attackType;
        //attackType = AttackType.B;
        Debug.Log($"{attackType} 공격 실행");
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
        if (Math.Abs(Distance) > pos.x + 0.5)
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
    }
    #endregion

    #region KickAttack

    #endregion

    #region SpinShardAttack
    #endregion

    #region JumpSlamAttack
    #endregion

    #region Ultimate
    public Node GetUltimateBT() { return UltimateAttack; }
    public bool CanTransitionToGroggy() { return parryCount >= 3; }
    #endregion

    #region Groggy
    public NodeState PlayAnimGroggy_Time(int num)
    {
        NodeState state = PlayAnim_Time(num, groggyDuration);
        if (state == NodeState.Success)
        {
            isEnranged = true;
            curEnrangedAtkSpeed = enrangedAtkSpeed_Mul;
            //Debug.Log("격노 시작");
        }
        return state;
    }
    //Enranged
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
