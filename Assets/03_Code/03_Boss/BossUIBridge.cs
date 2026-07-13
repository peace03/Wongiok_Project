using UnityEngine;

//Boss <-> UI Bridge
public class BossUIBridge : MonoBehaviour, IInitializable
{
    public int Priority => (int)InitOrder.Boss +10;

    private BossStatus bossStatus;
    private UIManager uiManager;
    private float bossMaxHP;        //보스 최대 체력
    private string bossName;
    private bool bossIsDead;        //보스 사망 여부

    public void Init()
    {
        bossStatus = ServiceLocator.Get<BossStatus>();
        uiManager = ServiceLocator.Get<UIManager>();
        bossMaxHP = bossStatus.BossMaxHP.FinalValue;
        bossName = bossStatus.gameObject.name;
        bossIsDead = false;
    }

    private void Start()
    {
        uiManager.ChangeScreen(UIScreenState.InGame);
        uiManager.SetBossHudVisible(true);
        PublishBossHudData(bossStatus.GetBossCurHP());

        Debug.Log($"[BossUIBridge] HUD 표시 요청 완료: {bossName}, " +
                  $"{bossStatus.GetBossCurHP()} / {bossMaxHP}");
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
        uiManager.SetBossHudVisible(false);
    }

    private void PublishBossHudData(float currentHP)
    {
        EventBus<UISetBossHudDataEvent>.Publish(
            new UISetBossHudDataEvent(bossName, currentHP, bossMaxHP));
    }
}
