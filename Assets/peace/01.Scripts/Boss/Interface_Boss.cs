//BossController(중개자)를 통해 State들에게 연결될 확장성 인터페이스
using UnityEngine;

public interface IBossLogics
{
    public bool GetStateDone(); //상태의 종료 여부
    public NodeState SetStateDone(bool set); //상태 여부 세팅
    public Node GetAttackBT(); //BT 노드 가져오기
    public AttackType GetAttackType();
    public void SetRandomPos(); //Idle 이동 좌표 지정
    public void InitCurTime_Idle(); //Idle 지속시간 초기화
    public void Spawn();
    public void IdleMove();
    public void ExcuteMove(); //이동 실행(FixedUpdate 실행용)
    public void AttackInit(); //공격 변수 초기화
}