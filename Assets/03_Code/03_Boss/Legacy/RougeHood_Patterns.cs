using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RougeHood_Patterns : BossPatternBase
{
    [Header("Test")]
    [SerializeField] private ExcuteAttackType_InGame excuteAttackType_InGame;

    [Header("Bullet")]
    [SerializeField] private BulletFactory bulletFactory;   //총알 오브젝트 풀 
    [SerializeField] private Transform startFirePos;        //총알이 발사 시작될 좌표
    
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
    [Tooltip("공격A 총알 발사 개수"), SerializeField, Min(1)] private int bulletNum = 1;
    [Tooltip("공격A 총알의 추가 관통 횟수. 0이면 첫 대상 명중 후 반납, -1이면 무한 관통이다."), SerializeField] private int A_bulletPenetrationCount = 0;
    [Tooltip("공격A 총알 발사 간격"), SerializeField, Min(0f)] private float fireRate = 0f;


    //Idle
    private Node aimedShotAttack;

    //Timer
    private float curTelegraphTime = 0f; //현재 사전신호 진행 시간

    // AttackA 연사 패턴은 BT가 매 프레임 호출하므로, 코루틴을 한 번만 시작하기 위한 상태값이다.
    private Coroutine fireBulletACoroutine;
    private bool isFiringBulletA;
    private bool isFireBulletACompleted;
    private bool didFireBulletAFail;

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
            //총알 연사: 코루틴이 bulletNum회 발사를 끝낼 때까지 Running을 유지한다.
            new Leaf(FireBulletAPattern),

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

    #region CommonLogic
    public override void LogicInit()
    {
        base.LogicInit();
        curTelegraphTime = 0f;
        ResetFireBulletAPattern();
        // 패턴이 중간에 끊기거나 다른 상태로 전환되어도 이전 조준선이 화면에 남지 않게 정리한다.
        laserSight?.StopAiming();
    }

    protected override void OnDisable()
    {
        // 보스가 비활성화되면 연사 코루틴도 함께 정리해 비활성 상태에서 총알이 나가지 않게 한다.
        ResetFireBulletAPattern();
        base.OnDisable();
    }
    #endregion

    #region Idle
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
    #endregion

    #region Attack
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

    //사전 신호 재생(레이저 사이트, 오소리)
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

    private NodeState FireBulletAPattern()
    {
        // Sequence는 완료된 자식을 다음 프레임에도 다시 평가하므로, 같은 공격에서 연사가 재시작되지 않게 막는다.
        if (isFireBulletACompleted)
            return didFireBulletAFail ? NodeState.Failure : NodeState.Success;

        // BT는 매 프레임 이 메서드를 호출한다. 첫 호출에서만 코루틴을 시작한다.
        if (fireBulletACoroutine == null)
        {
            isFiringBulletA = true;
            didFireBulletAFail = false;
            fireBulletACoroutine = StartCoroutine(FireBulletARoutine());
            return NodeState.Running;
        }

        // 코루틴이 발사 중이면 공격 시퀀스를 다음 단계로 넘기지 않는다.
        if (isFiringBulletA)
            return NodeState.Running;

        return NodeState.Running;
    }

    private IEnumerator FireBulletARoutine()
    {
        // WaitForSeconds는 게임 시간 기준이므로 히트스탑/불릿타임 중 발사 간격도 월드 속도에 맞춰 느려진다.
        WaitForSeconds fireInterval = new WaitForSeconds(fireRate);

        for (int shotIndex = 0; shotIndex < bulletNum; shotIndex++)
        {
            if (FireBullet() == NodeState.Failure)
            {
                didFireBulletAFail = true;
                break;
            }

            // 마지막 탄환 뒤에는 불필요하게 대기하지 않는다.
            if (shotIndex < bulletNum - 1 && fireInterval != null)
                yield return fireInterval;
        }

        isFiringBulletA = false;
        isFireBulletACompleted = true;
    }

    /// <summary>
    /// 공격 패턴이 사용할 총알 한 발을 풀에서 가져와 발사한다.
    /// 연사 횟수와 간격은 FireBulletA 코루틴이 담당하도록 분리한다.
    /// </summary>
    private NodeState FireBullet()
    {
        // 팩토리나 총구 소켓이 비어 있으면 발사할 수 없으므로 BT에 실패를 반환한다.
        if (bulletFactory == null || startFirePos == null)
        {
            Debug.LogWarning("[RougeHood] BulletFactory 또는 startFirePos가 설정되지 않았습니다.", this);
            return NodeState.Failure;
        }

        // AttackType.A의 실제 피해량은 BossStatus에서 계산한다.
        if (bossStatus == null)
        {
            Debug.LogWarning("[RougeHood] BossStatus를 찾지 못해 총알 피해량을 계산할 수 없습니다.", this);
            return NodeState.Failure;
        }

        Bullet bullet = bulletFactory.GetBullet();
        if (bullet == null)
            return NodeState.Failure;

        // Bullet은 ownerLayer에 속한 Collider를 무시한다.
        // gameObject.layer는 레이어 번호이므로 LayerMask로 전달하려면 비트 마스크로 변환해야 한다.
        LayerMask ownerLayer = 1 << gameObject.layer;

        // StartFire는 startFirePos의 위치와 forward 방향을 그대로 사용한다.
        bullet.StartFire(startFirePos, ownerLayer, bossStatus.GetAtkPower(AttackType.A), A_bulletPenetrationCount);
        return NodeState.Success;
    }

    private void ResetFireBulletAPattern()
    {
        if (fireBulletACoroutine != null)
            StopCoroutine(fireBulletACoroutine);

        fireBulletACoroutine = null;
        isFiringBulletA = false;
        isFireBulletACompleted = false;
        didFireBulletAFail = false;
    }
    #endregion
}
