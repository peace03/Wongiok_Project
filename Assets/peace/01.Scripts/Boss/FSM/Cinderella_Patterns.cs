using UnityEngine;
using System;
using System.Collections.Generic;

public enum AttackType { Kick, SpinShard, JumpSlam }
public enum Animation { Idle, Attack }

public class Cinderella_Patterns : MonoBehaviour, IInitializable, IBossLogics
{
    public int Priority => (int)InitOrder.Boss +1;

    [Header("Animator")]
    [SerializeField] private Animator anim;
    [Header("위치")]
    [SerializeField] private Transform spawnPos;
    [SerializeField] private Transform playerPos;
    [Header("Idle 상태")]
    [SerializeField] private float move_idleSpeed;
    [SerializeField] private Vector3 move_idlePos;
    [Header("Enranged 상태")]
    [SerializeField] private float move_enrangedSpeed;
    [SerializeField] private Vector3 move_enrangedPos;
    [Header("Attack 상태")]
    [SerializeField] private float kickChaseSpeed;
    [SerializeField] private Vector3 kickChasePos;

    public float Move_idleSpeed => move_idleSpeed;
    public Vector3 Move_idlePos => move_idlePos;
    public float Distance => playerPos.position.x - transform.position.x;
    public bool StateDone { get; private set; } //공격 BT 종료 여부

    private Rigidbody rb;
    private AnimatorStateInfo animState;
    private float curTime = 0f;

    private Node kickAttack;
    private Node spinShardAttack;
    private Node jumpSlamAttack;
    private AttackType attackType;  //현재 공격 타입
    private AttackType beforeType = AttackType.JumpSlam;  //이전 공격 타입

    //kick 공격 타입
    private bool chaseDone = false;    //추격 실행 여부
    private bool telegraphExcuted = false;
    private bool attackDone = false;    //공격 실행 여부
    

    private float x, y, z; //Move에 사용될 속도 저장

    public void Init()
    {
        rb = GetComponent<Rigidbody>();
        Init_BT();
    }

    public void Init_BT()
    {
        //마지막에 bool변수들 초기화 해줘야함
        kickAttack = new Sequence(new List<Node>
        {
            //추격 실렉터
            new Selector(new List<Node>
            {
                new ConditionLeaf(() => chaseDone), //추격이 끝났는가? -> 다음 시퀀스
                new Leaf(() => Chase(kickChasePos, kickChaseSpeed)) //해당 위치까지 이동
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
                new Sequence(new List<Node>
                {
                    new Leaf(() => PlayAnim_Speed((int)Animation.Attack, 1f)), //공격 애님 실행
                    new Leaf(() => {
                        attackDone = true;
                        return NodeState.Success; 
                    })
                })
            }),
            //후딜 시퀀스
            new Sequence(new List<Node>
            {
                new Leaf(() => PlayAnim_Time((int)Animation.Idle, 0.5f)), //Idle 애님 실행
                new Leaf(() => SetStateDone(true))
            })
        });
    }


    //private void Wait
    //private IEnumerator Wait()
    //{

    //}

    #region CommonLogic
    private void Move(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }
    public void ExcuteMove()
    {
        rb.linearVelocity = new Vector3(x, y, z);
    }
    //애니메이션 재생(속도 조절해서 시간을 맞춤)
    public NodeState PlayAnim_Speed(int num, float targetSeconds) 
    {
        //Debug.Log($"PlayAnim {num} 재생중");
        if (anim.GetInteger("Boss") != num) //애니메이션 전환
        {
            anim.SetInteger("Boss", num);
            return NodeState.Running; //방어코드(프레임 갱신)
        }
        if (anim.IsInTransition(0)) return NodeState.Running; //방어코드(애님 전환 중)
        //원하는 시간만큼 애님 재생
        animState = anim.GetCurrentAnimatorStateInfo(0);
        anim.speed = animState.length / targetSeconds;
        if (animState.normalizedTime < 0.95f) return NodeState.Running;
        else { anim.speed = 1.0f; return NodeState.Success; }
    }
    //애니메이션 재생(일정시간이 지나면 다음으로 넘어감)
    public NodeState PlayAnim_Time(int num, float targetSeconds) 
    {
        curTime += Time.deltaTime;
        //Debug.Log($"PlayAnim {num} 재생중");
        if (anim.GetInteger("Boss") != num) //애니메이션 전환
        {
            curTime = 0f;
            anim.SetInteger("Boss", num);
            return NodeState.Running; //방어코드(프레임 갱신)
        }
        if (anim.IsInTransition(0)) return NodeState.Running; //방어코드(애님 전환 중)
        //원하는 시간만큼 애님 재생
        animState = anim.GetCurrentAnimatorStateInfo(0);
        if (curTime < targetSeconds) return NodeState.Running;
        else { anim.speed = 1.0f; return NodeState.Success; }
    }
    public NodeState SetStateDone(bool set)
    {
        if(StateDone != set) StateDone = set;
        //Debug.Log($"SetStateDone {set}");
        return NodeState.Success;
    }
    public bool GetStateDone()
    {
        return StateDone;
    }
    #endregion

    #region Spawn
    public void Spawn() //SpawningState
    {
        transform.position = spawnPos.position;
    }
    #endregion

    #region Idle
    public void IdleMove()
    {
        if (Math.Abs(Distance) > move_idlePos.x + 0.5)
            Move(Math.Sign(Distance) * move_idleSpeed, 0, 0);
        else if (Math.Abs(Distance) < move_idlePos.x - 0.5)
            Move(-Math.Sign(Distance) * move_idleSpeed, 0, 0);
        else { Move(0, 0, 0); SetStateDone(true); }
    }
    #endregion

    #region Attack
    public Node GetAttackBT()
    {
        //while (attackType == beforeType) attackType = (AttackType)UnityEngine.Random.Range(0, 3);
        //beforeType = attackType;
        attackType = AttackType.Kick;
        //Debug.Log($"{attackType} 공격 실행");
        switch (attackType)
        {
            case AttackType.Kick:
                return kickAttack;
            case AttackType.SpinShard:
                return spinShardAttack;
            case AttackType.JumpSlam:
                return jumpSlamAttack;
        }
        return default;
    }

    private NodeState Chase(Vector3 pos, float speed)
    {
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

    public void AttackInit()
    {
        chaseDone = false;
        telegraphExcuted = false;
        attackDone = false;
    }
    #endregion

    #region KickAttack

    #endregion

    #region SpinShardAttack
    #endregion

    #region JumpSlamAttack
    #endregion

}
