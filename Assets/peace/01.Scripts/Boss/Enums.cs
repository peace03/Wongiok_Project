public enum State
{
    Spawn, Idle, Attack, Ultimate, Groggy, Defeated
}
public enum AttackType { A, B, C }
//애니메이션 transition 번호
public enum Animation 
{ 
    Idle,
    AttackA, AttackB, AttackC,
    Ultimate1, Ultimate2, Ultimate3,
    Chase, 
    Parry
}
//보스가 바라보는 방향
public enum Facing { Left, Right }
//각자 스크립트 초기화 순서 정해줄 때 사용
public enum InitOrder
{
    Player = 0,
    Skill = 100,
    Mob = 200,
    Boss = 300,
    UI = 400
}