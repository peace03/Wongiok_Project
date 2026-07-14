using System;

[Serializable]
public class Stat
{
    // 장비, 버프, 디버프가 적용되기 전의 기본 스탯 값입니다.
    public float BaseValue;

    // 고정 수치 증가량입니다. 예: 공격력 +5, 이동속도 +1 같은 효과를 누적합니다.
    private float additive;

    // 비율 증가량입니다. 기본값 1은 100%를 의미하며, 0.2를 더하면 최종값이 120%가 됩니다.
    private float multiplier = 1f;

    // 실제 게임 로직에서 사용하는 최종 스탯 값입니다.
    // 계산 순서: (기본값 + 고정 증가량) * 비율 증가량.
    public float FinalValue => (BaseValue + additive) * multiplier;
    public float AdditiveModifier => additive;
    public float Multiplier => multiplier;

    public Stat(float baseValue = 0f)
    {
        // 생성 시 기본값을 받아 초기 스탯을 설정합니다.
        BaseValue = baseValue;
    }

    public void SetBaseValue(float value)
    {
        // 캐릭터 기본 능력치나 초기 설정값을 갱신할 때 사용합니다.
        BaseValue = value;
    }

    public void AddValue(float value)
    {
        // 고정 수치 보정값을 누적합니다.
        // 음수를 넣으면 디버프처럼 수치를 낮출 수도 있습니다.
        additive += value;
    }

    public void AddMultiplier(float value)
    {
        // 비율 보정값을 누적합니다.
        // 예: value가 0.1이면 최종값이 10% 증가합니다.
        multiplier += value;
    }

    public void SetModifiers(float additiveModifier, float multiplierModifier)
    {
        additive = additiveModifier;
        multiplier = multiplierModifier;
    }

    public void ResetModifiers()
    {
        // 기본값은 유지하고, 전투 중 적용된 임시 보정값만 초기화합니다.
        additive = 0f;
        multiplier = 1f;
    }
}
