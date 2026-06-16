public enum State
{
    Spawn, Idle, Attack, Ultimate, Groggy, Defeated
}
public enum AttackType { A, B, C }
public enum Animation 
{ 
    Idle_L, Idle_R, 
    AttackA_L, AttackA_R, AttackB_L, AttackB_R, AttackC_L, AttackC_R,
    Ultimate1_L, Ultimate1_R, Ultimate2_L, Ultimate2_R, Ultimate3_L, Ultimate3_R,
    Chase 
}
//각자 스크립트 초기화 순서 정해줄 때 사용
public enum InitOrder
{
    Player = 0,
    Skill = 100,
    Mob = 200,
    Boss = 300,
    UI = 400
}