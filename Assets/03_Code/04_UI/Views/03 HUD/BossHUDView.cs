using UnityEngine;
using UnityEngine.UI;

// 보스 이름 및 HP 상태 표시 HUD View
public class BossHUDView : UIViewBase
{
    [Header("Boss Info")]
    [SerializeField] private Text bossNameText;

    [Header("HP")]
    [SerializeField] private Image hpFillImage;
    [SerializeField] private Text hpText;
    [SerializeField] private GameObject hpWarningObject;
    [SerializeField] private float hpWarningRatio = 0.2f;

    private string currentBossName = "Boss";
    private float currentHp = 1f;
    private float maxHp = 1f;

    protected override void Awake()
    {
        base.Awake();
        SubscirbeEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    protected override void OnShow()
    {
        RefreshAll();
    }

    protected override void OnHide()
    {
        if (hpWarningObject != null)
        {
            hpWarningObject.SetActive(false);
        }
    }

    public void SetBossInfo(string bossName)
    {
        currentBossName = string.IsNullOrEmpty(bossName) ? "Boss" : bossName;
        RefreshBossName();
    }

    public void SetBossHp(float currentHp, float maxHp)
    {
        this.maxHp = Mathf.Max(1f, maxHp);
        this.currentHp = Mathf.Clamp(currentHp, 0f, this.maxHp);

        RefreshHp();
    }

    public void ResetBossHud()
    {
        currentBossName = "Boss";
        currentHp = 1f;
        maxHp = 1f;

        RefreshAll();
    }

    private void SubscirbeEvents()
    {
        EventBus<UISetBossHudDataEvent>.action += HandleSetBossHudData;
        EventBus<UIResetEvent>.action += HandleReset;
    }

    private void UnsubscribeEvents()
    {
        EventBus<UISetBossHudDataEvent>.action -= HandleSetBossHudData;
        EventBus<UIResetEvent>.action -= HandleReset;
    }

    private void HandleSetBossHudData(UISetBossHudDataEvent eventData)
    {
        SetBossInfo(eventData.BossName);
        SetBossHp(eventData.CurrentHp, eventData.MaxHp);
    }

    private void HandleReset(UIResetEvent eventData)
    {
        ResetBossHud();
    }

    private void RefreshAll()
    {
        RefreshBossName();
        RefreshHp();
    }

    private void RefreshBossName()
    {
        if (bossNameText != null)
        {
            bossNameText.text = currentBossName;
        }
    }

    private void RefreshHp()
    {
        float ratio = Mathf.Clamp01(currentHp / maxHp);

        if (hpFillImage != null)
        {
            hpFillImage.fillAmount = ratio;
        }

        if (hpText != null)
        {
            hpText.text = $"{Mathf.CeilToInt(currentHp)} / {Mathf.CeilToInt(maxHp)}";
        }

        if (hpWarningObject != null)
        {
            hpWarningObject.SetActive(ratio <= hpWarningRatio);
        }
    }
}
