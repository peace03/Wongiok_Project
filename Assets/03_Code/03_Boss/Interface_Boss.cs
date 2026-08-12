//BossController(중개자)를 통해 State들에게 연결될 확장성 인터페이스
using UnityEngine;

public interface IBossLogics
{
    public bool IsParryed { get; } //이번 프레임에 패링 당했는가?
    public bool IsEnranged { get; } //격노 상태인가?
    public bool IsPhysicsOverridden { get; }
    public bool IsAttacking(); //현재 애니메이터 상태가 공격중인가?
    public bool GetStateDone(); //상태의 종료 여부
    public NodeState SetStateDone(bool set); //상태 여부 세팅
    public Node GetAttackBT(); //공격 BT 노드 가져오기
    public Node GetUltimateBT(); //궁극기 BT 노드 가져오기
    public AttackType GetAttackType();
    public string GetAttackId();
    public void SetRandomPos(); //Idle 이동 좌표 지정
    public void InitCurTime_Idle(); //Idle 지속시간 초기화
    public NodeState PlayAnimGroggy_Time(int num); //애니메이션 재생
    public void Spawn();
    public void IdleMove();
    public void EnrangedTimer();
    public void ExcuteMove(); //이동 실행(FixedUpdate 실행용)
    public void LogicInit(); //공격 변수 초기화
    public void StopGroggySfx(); //그로기 상태가 정상 종료되거나 강제로 중단될 때 반복 SFX 종료
    public bool CanTransitionToGroggy(); //패링 3회 성공시 그로기 전환 -> 추후 매개변수를 넣어 이 메서드 하나로 다른 상태로 전환 가능하도록 구현하기
}

public interface IBossAnimatorMoveHandler
{
    public void OnAnimatorMoveCallback();
}
