using UnityEngine;

public class BossAnimationKeyReceiver : MonoBehaviour, IInitializable
{
    public int Priority => (int)InitOrder.Boss+3;
    private IBossLogics bossPatternLogic;


    public void Init()
    {
        bossPatternLogic = ServiceLocator.Get<IBossLogics>();
    }

    //애니메이션 키로 이벤트 발생
    public void EnableParry()
    {
        // 현재 공격 ID와 함께 "지금부터 패링 시간"임을 알린다.
        EventBus<CanParryEvent>.Publish(
            new CanParryEvent(bossPatternLogic.GetAttackId(), true));
    }
    public void DisableParry()
    {
        // 패링 시간만 닫는다. 콜라이더는 이후 EnableAttack까지 켜 둬서 범위 정보를 유지한다.
        EventBus<CanParryEvent>.Publish(
            new CanParryEvent(bossPatternLogic.GetAttackId(), false));
    }
    public void EnableAttack()
    {
        // 이미 패링된 공격이거나 공격 애니메이션이 끝난 경우에는 피해 판정을 켜지 않는다.
        if (bossPatternLogic.IsParryed) return;
        if (!bossPatternLogic.IsAttacking()) return;

        // 같은 콜라이더를 RangeCheck 상태에서 실제 피해 상태로 전환한다.
        EventBus<ColliderToggleEvent>.
            Publish(new ColliderToggleEvent(bossPatternLogic.GetAttackId(), true));
    }
    public void DisableAttack()
    {
        // 공격 애니메이션의 피해 프레임이 끝났으므로 콜라이더와 범위 정보를 정리한다.
        EventBus<ColliderToggleEvent>.
            Publish(new ColliderToggleEvent(bossPatternLogic.GetAttackId(), false));
    }

    //Attack A의 타격 프레임에 호출
    //패링 등으로 이미 취소된 공격이면 VFX를 재생하지 않음
    public void PlayKickImpact()
    {
        if (bossPatternLogic == null) return;
        if (bossPatternLogic.IsParryed) return;
        if (!bossPatternLogic.IsAttacking()) return;

        EventBus<BossEffectEvent>.Publish(
            new BossEffectEvent(
                BossEffectCue.KickImpact,
                bossPatternLogic.GetAttackId()));
    }

    public void PlaySpinImpact()
    {
        if (bossPatternLogic == null) return;
        if (bossPatternLogic.IsParryed) return;
        if (!bossPatternLogic.IsAttacking()) return;

        EventBus<BossEffectEvent>.Publish(
            new BossEffectEvent(
                BossEffectCue.SpinImpact,
                bossPatternLogic.GetAttackId()));
    }

    public void PlaySlamImpact()
    {
        if (bossPatternLogic == null) return;
        if (bossPatternLogic.IsParryed) return;
        if (!bossPatternLogic.IsAttacking()) return;

        EventBus<BossEffectEvent>.Publish(
            new BossEffectEvent(
                BossEffectCue.SlamImpact,
                bossPatternLogic.GetAttackId()));
    }
}
