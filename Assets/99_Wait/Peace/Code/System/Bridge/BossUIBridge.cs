using UnityEngine;

//Boss <-> UI Bridge
public class BossUIBridge : MonoBehaviour, IInitializable
{
    public int Priority => (int)InitOrder.System + 2;

    private BossStatus bossStatus;
    private float bossMaxHP;        //보스 최대 체력
    private bool bossIsDead;        //보스 사망 여부

    public void Init()
    {
        bossStatus = ServiceLocator_Y.Get<BossStatus>();
        bossMaxHP = bossStatus.BossMaxHP.FinalValue;
    }

    private void OnEnable()
    {
        EventBus<BossHPChangedEvent>.action += BossHPChanged;
        EventBus<BossDeadEvent>.action += BossIsDead;
    }
    private void OnDisable()
    {
        EventBus<BossHPChangedEvent>.action -= BossHPChanged;
        EventBus<BossDeadEvent>.action -= BossIsDead;
    }

    //Update문에서 계속 검사하는건 낭비 같아서 한번만 실행하도록 만들었습니다.
    public void BossHPChanged(BossHPChangedEvent data)
    {
        float curBossHP = data.curHP;
        //여기서 UI 참조값을 통해 UI HP 업데이트 하시면 됩니다!
    }
    public void BossIsDead(BossDeadEvent data)
    {
        bossIsDead = true; //임시로 적은 것!
        //여기서 보스 죽었을 때 연출 실행하시면 됩니다!
    }
}
