using UnityEngine;

public class RougeHood_Patterns : BossPatternBase
{
    [Header("Test")]
    [SerializeField] private ExcuteAttackType_InGame excuteAttackType_InGame;

    [Header("Idle Distance")]
    [Tooltip("플레이어와 이 거리보다 가까우면 반대 방향으로 물러난다.")]
    [SerializeField, Min(0f)] private float idle_minDistance;
    [Tooltip("플레이어와 이 거리보다 멀어지면 플레이어 쪽으로 다가간다.")]
    [SerializeField, Min(0f)] private float idle_maxDistance;
    [Tooltip("맵 경계에서 이 정도 여유를 두고 바깥쪽 이동을 막는다.")]
    [SerializeField, Min(0f)] private float idle_groundEdgePadding = 0.5f;

    //Idle
    private Node aimedShotAttack;

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
        return CreateEmptyPatternNode();
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

    protected override NodeState PlayTelegraph(float baseTime)
    {
        // 현재 루주후드 공격은 골격 단계이므로 telegraph가 없어도 성공 처리한다.
        // 나중에 LaserSight나 RingDrawer를 쓰는 공격이 생기면 여기서 공통 예고 연출을 호출한다.
        if (telegraph != null) telegraph.SetActive(true);
        telegraphDrawer?.PlaySignal(GetAdjustedTelegraphTime(baseTime));
        telegraphExcuted = true;
        return NodeState.Success;
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
}
