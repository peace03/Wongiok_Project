using UnityEngine;

//패링 가능, 불가능 이벤트
public struct CanParryEvent 
{
    // 어느 공격의 패링 창인지 알려준다. 같은 보스라도 공격마다 다른 콜라이더를 찾아야 한다.
    public string AttackId { get; private set; }
    // true면 패링 시간 시작, false면 패링 시간 종료를 뜻한다.
    public bool CanParry { get; private set; }

    public CanParryEvent(string attackId, bool canParry)
    {
        AttackId = attackId;
        CanParry = canParry;
    }
}

//공격 콜라이더 토글 이벤트
public struct ColliderToggleEvent
{
    public string attackId { get; private set; }
    public bool state { get; private set; }
    public ColliderToggleEvent(string attackId, bool state)
    {
        this.attackId = attackId;
        this.state = state;
    }
}

//애니메이션 타이밍에 맞춰 재생할 보스 VFX 종류
public enum BossEffectCue { KickImpact, SpinImpact, SlamImpact, UltimateImpact }
//보스 VFX 재생 요청 이벤트
public struct BossEffectEvent
{
    public BossEffectCue Cue { get; private set; }
    public string AttackId { get; private set; }
    public int Variant { get; private set; }
    public BossEffectEvent(BossEffectCue cue, string attackId, int variant = 0)
    {
        Cue = cue;
        AttackId = attackId;
        Variant = variant;
    }
}

//보스의 시점 방향에 따라 콜라이더 위치 변경
public struct BossFacingChangeEvent
{
    public Facing dir { get; private set; }
    public BossFacingChangeEvent(Facing dir) { this.dir = dir; }
}

//공격 종료 이벤트 (공격 콜라이더가 플레이어 1회만 공격하도록 기억)
public struct AttackFinishEvent { }

//궁극기 발동
public struct UltimateInvokeEvent { }

//SpinShard 공격 장판 스폰
public struct OnShardHitBoxEvent
{
    public Transform pos { get; private set; }
    public OnShardHitBoxEvent(Transform pos)
    {
        this.pos = pos;
    }
}

//보스 체력 변화 이벤트(UI 소통용)
public struct BossHPChangedEvent
{
    public float curHP { get; private set; }
    public BossHPChangedEvent(float curHP)
    {
        this.curHP = curHP;
    }
}
//보스 죽음 이벤트(UI 연출 시작용)
public struct BossDeadEvent { }

//보스 사망 연출 완료 이벤트(UI 종료용)
public struct BossDeathPresentationFinishedEvent { }
