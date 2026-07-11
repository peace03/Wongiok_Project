using System;

[Serializable]
public class LivingStatus
{
    // 생명체가 공통으로 가지는 스탯들입니다. 
    // PlayerStatusData처럼 상속받는 클래스에서 추가 스탯을 확장할 수 있습니다.
    public Stat MaxHP = new Stat();
    public Stat AttackPower = new Stat();
    public Stat MoveSpeed = new Stat();
    public Stat AttackSpeed = new Stat();
    public Stat Cooldown = new Stat();

    // 현재 체력입니다. MaxHP.FinalValue와 별도로 관리해서 피해/회복을 누적합니다.
    public float CurrentHP;

    // 체력이 0 이하가 되면 사망 상태로 판단합니다.
    public bool IsDead => CurrentHP <= 0f;

    public void Init()
    {
        // 초기화 시 현재 체력을 최종 최대 체력으로 채웁니다.
        CurrentHP = MaxHP.FinalValue;
    }

    public virtual void ResetAllModifiers()
    {
        // 기본 스탯값은 유지하고, 각 스탯에 붙은 임시 보정값만 제거합니다.
        MaxHP.ResetModifiers();
        AttackPower.ResetModifiers();
        MoveSpeed.ResetModifiers();
        AttackSpeed.ResetModifiers();
        Cooldown.ResetModifiers();
    }
}
