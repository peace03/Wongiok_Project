using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class Cinderella_Patterns : MonoBehaviour, IInitializable, IBossLogics
{
    public int Priority => (int)InitOrder.Boss + 1;

    [Header("Animator")]
    [SerializeField] private Animator anim;

    [Header("Position")]
    [SerializeField] private Transform spawnPos;
    [SerializeField] private Transform playerPos;

    [Header("Idle")]
    [SerializeField] private float idleDurationTime;
    [Min(0f)] [SerializeField] private float move_idleSpeed;
    [Min(0f)] [SerializeField] private float move_idleRange;
    [Min(3f)] [SerializeField] private float move_idlePos;

    [Header("Enranged")]
    [SerializeField] private float durationEnranged;
    [SerializeField] private float enrangedAtkSpeed_Mul = 1f;

    [Header("Attack")]
    [SerializeField] private float kickChaseSpeed;
    [SerializeField] private Vector3 kickChasePos;
    [SerializeField] private float postAtkDelay;

    [Header("Ultimate")]
    [SerializeField] private float UltimateChaseSpeed;
    [SerializeField] private Vector3 UltimateChasePos;

    [Header("Groggy")]
    [SerializeField] private float groggyDuration;

    public float Distance => playerPos == null ? 0f : playerPos.position.x - transform.position.x;
    public bool StateDone { get; private set; }
    public bool IsParryed => isParryed;
    public bool IsEnranged => isEnranged;

    private Transform bossSkin;
    private Rigidbody rb;
    private AnimatorStateInfo animState;
    private float curTime_Anim;
    private float curTime_Idle;
    private bool isParryed;

    private Node kickAttack;
    private Node spinShardAttack;
    private Node jumpSlamAttack;
    private Node UltimateAttack;
    private BossAttackType attackType;
    private BossFacing curFacing = BossFacing.Left;

    private readonly float[] rotValue =
    {
        228f, -228f,
        -90f, 90f, 0f, 0f, 0f, 0f,
        -90f, 90f, -90f, 90f, -90f, 90f,
        -90f, 90f,
        228f, -228f,
        -90f, 90f
    };

    private Vector3 RandomPos;
    private int parryCount;
    private int comboStep;
    private bool isEnranged;
    private float curEnrangedTime;
    private float curEnrangedAtkSpeed = 1f;
    private float x;
    private float y;
    private float z;

    public void Init()
    {
        // 보스 패턴에서 사용하는 참조와 BT 노드를 준비합니다.
        bossSkin = transform.childCount > 0 ? transform.GetChild(0) : transform;
        rb = GetComponent<Rigidbody>();
        Init_BT();

        if (move_idleRange >= move_idlePos)
            move_idleRange = move_idlePos - 0.5f;
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
        // 깨진 패턴 파일을 복구하기 위한 기본 BT입니다. 세부 패턴은 각 공격 노드 안에서 확장합니다.
        kickAttack = new Leaf(() => RunSimpleAttack(BossAnimation.AttackA));
        spinShardAttack = new Leaf(() => RunSimpleAttack(BossAnimation.AttackB));
        jumpSlamAttack = new Leaf(() => RunSimpleAttack(BossAnimation.AttackC));
        UltimateAttack = new Leaf(RunSimpleUltimate);
    }

    private NodeState RunSimpleAttack(BossAnimation animation)
    {
        if (isParryed)
        {
            Parryed();
            SetStateDone(true);
            return NodeState.Success;
        }

        UpdateFacing();

        NodeState result = PlayAnim_Time((int)animation, postAtkDelay);
        if (result != NodeState.Success) return result;

        SetStateDone(true);
        return NodeState.Success;
    }

    private NodeState RunSimpleUltimate()
    {
        if (isParryed)
        {
            Parryed();
            parryCount++;
            comboStep++;
            return NodeState.Success;
        }

        UpdateFacing();

        NodeState result = PlayAnim_Time((int)BossAnimation.Ultimate1, postAtkDelay);
        if (result != NodeState.Success) return result;

        SetStateDone(true);
        return NodeState.Success;
    }

    private void ParryKeyDown(ParryKeyDown data)
    {
        isParryed = true;
    }

    private void Parryed()
    {
        isParryed = false;
        EventBus<CanParryEvent>.Publish(new CanParryEvent(false));
        EventBus<ColliderToggleEvent>.Publish(new ColliderToggleEvent(attackType, false));
    }

    public bool IsAttacking()
    {
        if (anim == null) return false;

        int currentAnim = anim.GetInteger("Boss");
        return currentAnim == (int)BossAnimation.AttackA
            || currentAnim == (int)BossAnimation.AttackB
            || currentAnim == (int)BossAnimation.AttackC
            || currentAnim == (int)BossAnimation.Ultimate1
            || currentAnim == (int)BossAnimation.Ultimate2
            || currentAnim == (int)BossAnimation.Ultimate3;
    }

    private void Move(float nextX, float nextY, float nextZ)
    {
        x = nextX;
        y = nextY;
        z = nextZ;
    }

    public void ExcuteMove()
    {
        if (rb == null) return;

        rb.linearVelocity = new Vector3(x, y, z);
    }

    public void UpdateFacing()
    {
        BossFacing newFacing = Distance < 0f ? BossFacing.Left : BossFacing.Right;
        if (curFacing == newFacing) return;

        curFacing = newFacing;
        EventBus<BossFacingChangeEvent>.Publish(new BossFacingChangeEvent(curFacing));
    }

    public void SyncFacingWithAnim(int targetAnim)
    {
        if (anim == null) return;

        bool shouldMirror = curFacing == BossFacing.Right;
        anim.SetBool("isMirrored", shouldMirror);
        anim.SetInteger("Boss", targetAnim);

        if (bossSkin == null) return;

        int angleIndex = curFacing == BossFacing.Left ? targetAnim * 2 : targetAnim * 2 + 1;
        if (angleIndex < 0 || angleIndex >= rotValue.Length) return;

        bossSkin.rotation = Quaternion.Euler(0f, rotValue[angleIndex], 0f);
    }

    public bool IsAnimationReady(int targetAnim)
    {
        if (anim == null) return true;

        int curAnim = anim.GetInteger("Boss");
        bool curMirror = anim.GetBool("isMirrored");
        bool targetMirror = curFacing == BossFacing.Right;

        if (curAnim != targetAnim || curMirror != targetMirror)
        {
            curTime_Anim = 0f;
            SyncFacingWithAnim(targetAnim);
            return false;
        }

        return !anim.IsInTransition(0);
    }

    public NodeState PlayAnim_Speed(int num, float targetSeconds)
    {
        if (!IsAnimationReady(num)) return NodeState.Running;
        if (anim == null) return NodeState.Success;

        animState = anim.GetCurrentAnimatorStateInfo(0);
        float targetSpeed = Mathf.Approximately(targetSeconds, 0f)
            ? 1f
            : animState.length / targetSeconds;

        if (isEnranged && IsAttackAnimation(num))
            targetSpeed *= curEnrangedAtkSpeed;

        anim.speed = targetSpeed;

        if (animState.normalizedTime < 0.95f) return NodeState.Running;

        anim.speed = 1f;
        return NodeState.Success;
    }

    public NodeState PlayAnim_Time(int num, float targetSeconds)
    {
        if (!IsAnimationReady(num)) return NodeState.Running;
        if (anim == null) return NodeState.Success;

        curTime_Anim += Time.deltaTime;
        animState = anim.GetCurrentAnimatorStateInfo(0);

        if (curTime_Anim < targetSeconds) return NodeState.Running;

        anim.speed = 1f;
        curTime_Anim = 0f;
        return NodeState.Success;
    }

    public NodeState SetStateDone(bool set)
    {
        StateDone = set;
        return NodeState.Success;
    }

    public bool GetStateDone()
    {
        return StateDone;
    }

    public void Spawn()
    {
        if (spawnPos == null) return;

        transform.position = spawnPos.position;
    }

    public void InitCurTime_Idle()
    {
        curTime_Idle = 0f;
    }

    public void SetRandomPos()
    {
        if (playerPos == null)
        {
            RandomPos = transform.position;
            return;
        }

        float element = move_idlePos + Random.Range(-move_idleRange, move_idleRange);
        float direction = Math.Sign(Distance) <= 0 ? 1f : -1f;
        RandomPos = new Vector3(playerPos.position.x + element * direction, transform.position.y, transform.position.z);
    }

    public void IdleMove()
    {
        UpdateFacing();
        PlayAnim_Time((int)BossAnimation.Idle, 1f);

        float delta = RandomPos.x - transform.position.x;
        if (Mathf.Abs(delta) <= 0.5f)
        {
            Move(0f, 0f, 0f);
            curTime_Idle += Time.deltaTime;
        }
        else
        {
            Move(Math.Sign(delta) * move_idleSpeed, 0f, 0f);
        }

        if (curTime_Idle > idleDurationTime)
            SetStateDone(true);
    }

    public BossAttackType GetAttackType()
    {
        return attackType;
    }

    public Node GetAttackBT()
    {
        attackType = BossAttackType.A;
        return attackType switch
        {
            BossAttackType.A => kickAttack,
            BossAttackType.B => spinShardAttack,
            BossAttackType.C => jumpSlamAttack,
            _ => kickAttack
        };
    }

    private NodeState Chase(Vector3 pos, float speed, int animNum)
    {
        UpdateFacing();
        if (!IsAnimationReady(animNum))
        {
            Move(0f, 0f, 0f);
            return NodeState.Running;
        }

        if (Math.Abs(Distance) > pos.x + 0.5f)
        {
            Move(Math.Sign(Distance) * speed, 0f, 0f);
            return NodeState.Running;
        }

        Move(0f, 0f, 0f);
        return NodeState.Success;
    }

    public void LogicInit()
    {
        parryCount = 0;
        comboStep = 0;
        curTime_Anim = 0f;
    }

    public Node GetUltimateBT()
    {
        return UltimateAttack;
    }

    public bool CanTransitionToGroggy()
    {
        return parryCount >= 3;
    }

    public NodeState PlayAnimGroggy_Time(int num)
    {
        NodeState state = PlayAnim_Time(num, groggyDuration);
        if (state != NodeState.Success) return state;

        isEnranged = true;
        curEnrangedAtkSpeed = enrangedAtkSpeed_Mul;
        return NodeState.Success;
    }

    public void EnrangedTimer()
    {
        curEnrangedTime += Time.deltaTime;
        if (curEnrangedTime < durationEnranged) return;

        isEnranged = false;
        curEnrangedTime = 0f;
        curEnrangedAtkSpeed = 1f;
        Debug.Log("보스 분노 종료");
    }

    private bool IsAttackAnimation(int num)
    {
        return num == (int)BossAnimation.AttackA
            || num == (int)BossAnimation.AttackB
            || num == (int)BossAnimation.AttackC;
    }
}
