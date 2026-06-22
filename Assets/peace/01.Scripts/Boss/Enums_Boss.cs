public enum State
{
    Spawn, Idle, Attack, Ultimate, Groggy, Defeated
}
public enum AttackType { A, B, C, C_2 }
//애니메이션 transition 번호
public enum Animation 
{ 
    Idle,
    AttackA, AttackB, AttackC,
    Ultimate1, Ultimate2, Ultimate3,
    Chase, 
    Parry,
    Groggy
}
//보스가 바라보는 방향
public enum Facing { Left, Right }
