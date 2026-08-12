using UnityEngine;

public enum MonsterDeathCause
{
    PlayerAttack,
    SelfDestruct,
    Environment
}

public readonly struct MonsterDeadEvent
{
    public readonly GameObject MonsterObject;
    public readonly MonsterDeathCause Cause;

    public MonsterDeadEvent(
        GameObject monsterObject,
        MonsterDeathCause cause = MonsterDeathCause.PlayerAttack)
    {
        MonsterObject = monsterObject;
        Cause = cause;
    }
}
