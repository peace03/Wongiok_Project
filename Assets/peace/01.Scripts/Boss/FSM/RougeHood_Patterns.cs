using UnityEngine;

public class RougeHood_Patterns : BossPatternBase
{
    [Header("Test")]
    [SerializeField] private ExcuteAttackType_InGame excuteAttackType_InGame;

    private Node emptyAttack;

    protected override void BuildPatterns()
    {
        emptyAttack = CreateEmptyPatternNode();
        ultimateAttack = CreateEmptyPatternNode();
    }

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
        return emptyAttack;
    }
}
