// BossController가 각 보스 상태에 넘겨주는 보스 패턴 로직 인터페이스입니다.
public interface IBossLogics
{
    bool IsParryed { get; }
    bool IsEnranged { get; }

    bool IsAttacking();
    bool GetStateDone();
    NodeState SetStateDone(bool set);
    Node GetAttackBT();
    Node GetUltimateBT();
    BossAttackType GetAttackType();

    void SetRandomPos();
    void InitCurTime_Idle();
    NodeState PlayAnimGroggy_Time(int num);
    void Spawn();
    void IdleMove();
    void EnrangedTimer();
    void ExcuteMove();
    void LogicInit();
    bool CanTransitionToGroggy();
}
