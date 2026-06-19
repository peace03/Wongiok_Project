using System.Collections.Generic;
using UnityEngine;

public class BossController : MonoBehaviour, IInitializable
{
    // 보스 상태 초기화는 보스 스탯과 패턴 로직 이후에 실행합니다.
    public int Priority => (int)InitOrder.Boss + 2;

    private IBossLogics logics;
    private BossStatus status;
    private BossState curState;

    public Dictionary<BossStateId, BossState> BossStates { get; }
        = new Dictionary<BossStateId, BossState>();

    public void Init()
    {
        status = ServiceLocator.Get<BossStatus>();
        logics = GetComponent<IBossLogics>();

        BossStates.Clear();
        BossStates.Add(BossStateId.Spawn, new SpawningState(this, logics));
        BossStates.Add(BossStateId.Idle, new BossIdleState(this, logics));
        BossStates.Add(BossStateId.Attack, new AttackingState(this, logics));
        BossStates.Add(BossStateId.Ultimate, new UltimateCastingState(this, logics));
        BossStates.Add(BossStateId.Groggy, new GroggyState(this, logics));
        BossStates.Add(BossStateId.Defeated, new DefeatedState(this, logics));

        curState = BossStates[BossStateId.Spawn];
        curState.Enter();
    }

    private void OnEnable()
    {
        EventBus<UltimateInvoke>.action += SetUltimateState;
    }

    private void OnDisable()
    {
        EventBus<UltimateInvoke>.action -= SetUltimateState;
    }

    private void FixedUpdate()
    {
        curState?.FixedUpdate();
    }

    private void Update()
    {
        curState?.Update();

        // 테스트용 임시 데미지 입력입니다.
        if (Input.GetKeyDown(KeyCode.Space))
            status.TakeDamage(20f);
    }

    public void ChangeState(BossStateId state)
    {
        if (!BossStates.TryGetValue(state, out BossState nextState)) return;

        curState?.Exit();
        curState = nextState;
        curState.Enter();
    }

    public void SetUltimateState(UltimateInvoke data)
    {
        ChangeState(BossStateId.Ultimate);
    }
}
