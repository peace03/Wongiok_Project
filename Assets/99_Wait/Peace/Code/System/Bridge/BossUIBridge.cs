using UnityEngine;

//Boss <-> UI Bridge
public class BossUIBridge : MonoBehaviour, IInitializable
{
    public int Priority => (int)InitOrder.Boss +10;

    private BossStatus bossStatus;
    private float bossMaxHP;        //보스 최대 체력
    private string bossName;
    private bool bossIsDead;        //보스 사망 여부

    public void Init()
    {
        bossStatus = ServiceLocator.Get<BossStatus>();
        bossMaxHP = bossStatus.BossMaxHP.FinalValue;
        bossName = bossStatus.gameObject.name;
        bossIsDead = false;
    }

    private void Start()
    {
        EventBus<UIChangeScreenEvent>.Publish(
            new UIChangeScreenEvent(UIScreenState.InGame));

        EventBus<UISetBossHudVisibleEvent>.Publish(new UISetBossHudVisibleEvent(true));
        PublishBossHudData(bossStatus.GetBossCurHP());
    }

    private void OnEnable()
    {
        EventBus<BossHPChangedEvent>.action += BossHPChanged;
        EventBus<BossDeadEvent>.action += BossIsDead;
        EventBus<BossDeathPresentationFinishedEvent>.action += BossDeathPresentationFinished;
    }
    private void OnDisable()
    {
        EventBus<BossHPChangedEvent>.action -= BossHPChanged;
        EventBus<BossDeadEvent>.action -= BossIsDead;
        EventBus<BossDeathPresentationFinishedEvent>.action -= BossDeathPresentationFinished;
    }

    //Update문에서 계속 검사하는건 낭비 같아서 한번만 실행하도록 만들었습니다.
    public void BossHPChanged(BossHPChangedEvent data)
    {
        if (bossIsDead)
            return;

        PublishBossHudData(data.curHP);
    }

    public void BossIsDead(BossDeadEvent data)
    {
        if (bossIsDead)
            return;

        bossIsDead = true;
        PublishBossHudData(0f);
    }

    private void BossDeathPresentationFinished(BossDeathPresentationFinishedEvent data)
    {
        EventBus<UISetBossHudVisibleEvent>.Publish(new UISetBossHudVisibleEvent(false));
    }

    private void PublishBossHudData(float currentHP)
    {
        EventBus<UISetBossHudDataEvent>.Publish(
            new UISetBossHudDataEvent(bossName, currentHP, bossMaxHP));
    }
}
