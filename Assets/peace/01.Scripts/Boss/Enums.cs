public enum State
{
    Spawn, Idle, Attack, Ultimate, Groggy, Defeated
}
public enum AttackType { A, B, C }
//애니메이션 transition 번호
public enum Animation 
{ 
    Idle_L, Idle_R, 
    AttackA_L, AttackA_R, AttackB_L, AttackB_R, AttackC_L, AttackC_R,
    Ultimate1_L, Ultimate1_R, Ultimate2_L, Ultimate2_R, Ultimate3_L, Ultimate3_R,
    Chase_L, Chase_R, //Chase는 애님 1개인데 알고리즘 상 짝수 맞춰줘야 편함
    Parry_L, Parry_R
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