using UnityEngine;
using System.Collections.Generic;

public class RougeHood_Patterns : BossPatternBase
{
    [Header("Test")]
    [SerializeField] private ExcuteAttackType_InGame excuteAttackType_InGame;
    
    [Header("Telegraph")]
    [SerializeField] private LaserSight laserSight;

    [Header("Idle Distance")]
    [Tooltip("플레이어와 이 거리보다 가까우면 반대 방향으로 물러난다.")]
    [SerializeField, Min(0f)] private float idle_minDistance;
    [Tooltip("플레이어와 이 거리보다 멀어지면 플레이어 쪽으로 다가간다.")]
    [SerializeField, Min(0f)] private float idle_maxDistance;
    [Tooltip("맵 경계에서 이 정도 여유를 두고 바깥쪽 이동을 막는다.")]
    [SerializeField, Min(0f)] private float idle_groundEdgePadding = 0.5f;

    [Header("AttackA")]
    [Tooltip("레이저 사이트 지속시간"), SerializeField] private float laserSightDuration;
    [Tooltip("공격 전 몇초부터 레이저사이트 깜빡이는지 설정"), SerializeField] private float blinkTimingBeforeAttack;

    //Idle
    private Node aimedShotAttack;

    //Timer
    private float curTelegraphTime = 0f; //현재 사전신호 진행 시간

    protected override void BuildPatterns()
    {
        aimedShotAttack = BuildAimedShotAttack();
        ultimateAttack = CreateEmptyPatternNode();
    }

    #region BT
    private Node BuildAimedShotAttack()
    {
        //공격 시퀀스
        // 레퍼런스가 들어오기 전까지는 Attack 상태가 멈추지 않도록 즉시 종료 노드를 반환한다.
        // 실제 조준 사격 BT가 구현되면 이 메서드 안에 Chase/Telegraph/Attack/PostDelay 순서를 조립한다.
        return new Sequence(new List<Node>
        {
            //플레이어와 거리 검사 -> 4m이내 접근시 공격B 전환
            //사전신호: 레이저사이트
            new Leaf(() => PlayTelegraph(laserSightDuration, TelegraphType.LaserSight)),
            //총알 발사

            //후딜
            new Leaf(() =>
            {
                Debug.Log("실행됨");
                curTelegraphTime = 0f;
                return SetStateDone(true);
            })
        });
        //return CreateEmptyPatternNode();
    }
    #endregion

    protected override AttackType SelectAttack()
    {
        return excuteAttackType_InGame switch
        {
            ExcuteAttackType_InGame.A => AttackType.A,
            ExcuteAttackType_InGame.B => AttackType.B,
            ExcuteAttackType_InGame.C => AttackType.C,
            _ => AttackType.A
        };
    }

    protected override Node GetAttackNode(AttackType selectedAttackType)
    {
        return aimedShotAttack;
    }

    protected override NodeState PlayTelegraph(float baseTime, TelegraphType type)
    {
        // 나중에 LaserSight나 GroundMarker를 쓰는 공격이 생기면 여기서 사전신호를 호출한다.
        if (type == TelegraphType.LaserSight) //레이저 사이트 사전신호
        {
            if (type == TelegraphType.LaserSight && curTelegraphTime == 0f) //레이저 사이트 사전신호 발생
            {
                laserSight.StartAiming();
            }
            curTelegraphTime += Time.deltaTime;
            if(curTelegraphTime > baseTime - blinkTimingBeforeAttack && laserSight.GetAimLock() == false) //깜빡임 시작
            {
                laserSight.LockAim();
            }
            if (curTelegraphTime > baseTime) //지정시간 지나면 레이저사이트 비활성화
            {
                laserSight.StopAiming();
                return NodeState.Success;
            }
            return NodeState.Running;
        }
        return NodeState.Failure;
    }

    public override void IdleMove()
    {
        UpdateFacing();
        curTime_Idle += Time.deltaTime;
        //나중에 맵 가장자리에 몰렸을 때 B패턴 실행하도록 구현

        //float minDistance = Mathf.Min(idle_minDistance, idle_maxDistance);
        //float maxDistance = Mathf.Max(idle_minDistance, idle_maxDistance);
        float absDistance = Mathf.Abs(Distance);
        float dirToPlayer = GetDirectionToPlayer();
        float moveDirX = 0f;

        // 최소거리보다 가까우면 플레이어 반대편으로 물러난다.
        // dirToPlayer가 플레이어 방향이므로 -dirToPlayer가 후퇴 방향이다.
        if (absDistance < idle_minDistance)
            moveDirX = -dirToPlayer;
        // 최대거리보다 멀면 사격/추격 가능 거리를 유지하기 위해 플레이어 쪽으로 다가간다.
        else if (absDistance > idle_maxDistance)
            moveDirX = dirToPlayer;
        // 최소거리 이상, 최대거리 이하라면 현재 위치가 루주후드의 적정 전투 거리이므로 Idle을 유지한다.
        else
            moveDirX = 0f;

        // 맵 경계 밖으로 나가는 방향이면 이동을 정지한다.
        // 이때 SyncLocomotionAnim(0)이 호출되어 걷기에서 Idle로 자연스럽게 전환된다.
        moveDirX = BlockMoveOutsideGround(moveDirX, idle_groundEdgePadding);

        Move(moveDirX * move_idleSpeed, 0, 0);
        SyncLocomotionAnim(moveDirX); // 애니메이션은 base 캐시가 값 변경 시에만 갱신한다.

        if (curTime_Idle > idleDurationTime)
            SetStateDone(true);
    }

    #region CommonLogic
    public override void LogicInit()
    {
        base.LogicInit();
        curTelegraphTime = 0f;
        // 패턴이 중간에 끊기거나 다른 상태로 전환되어도 이전 조준선이 화면에 남지 않게 정리한다.
        laserSight?.StopAiming();
    }
    #endregion
}
