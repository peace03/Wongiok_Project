public enum State
{
    Spawn, Idle, Attack, Ultimate, Groggy, Defeated
}
//실행시 반복할 공격 타입
public enum ExcuteAttackType_InGame { A, B, C, ALL }
//공격 타입
public enum AttackType { A, B, C, C_2, D }
//애니메이션 transition 번호
public enum Animation 
{ 
    Idle,
    AttackA, AttackB, AttackC,
    Ultimate1, Ultimate2, Ultimate3,
    Chase, 
    Parry,
    Groggy,
    Walking
}
//보스가 바라보는 방향
public enum Facing { Left, Right }
