using UnityEngine;
using System;
using System.Collections.Generic;

public enum AttackType { Kick, SpinShard, JumpSlam }

public class Cinderella_Patterns : MonoBehaviour, IInitializable, IBossLogics
{
    public int Priority => (int)InitOrder.Boss +1;

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
    private Rigidbody rb;

    private Node kickAttack;
    private Node spinShardAttack;
    private Node jumpSlamAttack;
    private AttackType attackType;  //현재 공격 타입
    private AttackType beforeType = AttackType.JumpSlam;  //이전 공격 타입
    private bool chaseDone = false;    //추격 실행 여부

    private float x, y, z; //Move에 사용될 속도 저장

    public void Init()
    {
        rb = GetComponent<Rigidbody>();
        Init_BT();
    }

    public void Init_BT()
    {
        kickAttack = new Selector(new List<Node>
        {
            //추격 시퀀스
            new Sequence(new List<Node>
            {
                new ConditionLeaf(() => !chaseDone), //추격이 끝났는가? -> 다음 시퀀스
                new ConditionLeaf(DistanceCheck), //사정거리 안에 있는가? -> 아니면 이동
                new Leaf(() => Chase(kickChasePos, kickChaseSpeed)) //해당 위치까지 이동
            }),
            //사전신호 시퀀스
            new Sequence(new List<Node>
            {

            }),
            //공격 시퀀스
            new Sequence(new List<Node>
            {

            }),
            //후딜 시퀀스
            new Sequence(new List<Node>
            {

            })
        });
    }

    #region Spawn
    public void Spawn() //SpawningState
    {
        transform.position = spawnPos.position;
    }
    #endregion

    private void Move(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    #region Idle
    public void ExcuteIdleMove()
    {
        if (Math.Abs(Distance) > move_idlePos.x + 0.5)
            Move(Math.Sign(Distance) * move_idleSpeed, 0, 0);
        else if (Math.Abs(Distance) < move_idlePos.x - 0.5)
            Move(-Math.Sign(Distance) * move_idleSpeed, 0, 0);
        else Move(0, 0, 0);
    }
    #endregion

    #region Attack
    public Node GetAttackBT()
    {
        //while (attackType == beforeType) attackType = (AttackType)UnityEngine.Random.Range(0, 3);
        //beforeType = attackType;
        attackType = AttackType.Kick;
        Debug.Log($"{attackType} 공격 실행");
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
            Debug.Log("오른쪽 호출");
            Move(Math.Sign(Distance) * speed, 0, 0);
            return NodeState.Running;
        }
        else return NodeState.Success;
    }

    private bool DistanceCheck()
    {
        if (Math.Abs(Distance) < kickChasePos.x + 0.5) //사정거리 안에 있다면
        {
            Move(0, 0, 0);
            chaseDone = true;
            return false; //실패 반환하기 위함
        }
        else return true; //성공 반환 -> 다음 leaf노드 실행
    }
    public void ExcuteAttackMove()
    {
        rb.linearVelocity = new Vector3(x, y, z);
    }
    #endregion

    #region KickAttack
    public void Telegraph()
    {

    }
    #endregion

    #region SpinShardAttack
    #endregion

    #region JumpSlamAttack
    #endregion

}
