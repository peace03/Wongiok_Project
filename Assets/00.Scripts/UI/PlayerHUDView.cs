using UnityEngine;
using UnityEngine.UI;

// 플레이어의 기본 상태를 표시하는 HUD View
// HP, EXP, 레벨, 목숨, 스킬 슬롯, 회복 아이템 등 화면에 보여주는 역할만 담당
public class PlayerHUDView : UIViewBase
{
    [Header("Level")]
    [SerializeField] private Text levelText;

    [Header("EXP")]
    [SerializeField] private Image expFillImage;
    [SerializeField] private Text expText;

    [Header("HP")]
    [SerializeField] private Image hpFillImage;
    [SerializeField] private Text hpText;
    [SerializeField] private GameObject hpWarningObject;
    [SerializeField] private float hpWarningRatio = 0.2f;

    [Header("Life")]
    [SerializeField] private Image[] lifeIcons;

    [Header("Skill Slots")]
    [SerializeField] private Image[] skillIconImages;
    [SerializeField] private Image[] skillCooldownFillImages;
    [SerializeField] private Text[] skillKeyTexts;
    [SerializeField] private Text[] skillLevelTexts;

    [Header("Heal Item")]
    [SerializeField] private Image healItemIconImage;
    [SerializeField] private Text healItemCountText;

    private int currentLevel = 1;
    private float currentExp;
    private float requiredExp = 1f;
    private float currentHp = 1f;
    private float maxHp = 1f;
    private int currentLife;
    private int maxLife;
    private int healItemCount;
    private int maxHealItemCount;

    protected override void Awake()
    {
        base.Awake();
        SubscribeEvents();
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
    }

    public void ResetHudView()
    {
        currentLevel = 1;
        currentExp = 0f;
        requiredExp = 1f;
        currentHp = 1f;
        maxHp = 1f;
        currentLife = 0;
        maxLife = 0;
        healItemCount = 0;
        maxHealItemCount = 0;

        RefreshAll();
    }

    private void SubscribeEvents()
    {
        EventBus<UISetPlayerLevelEvent>.action += HandleSetPlayerLevel;
        EventBus<UISetPlayerExpEvent>.action += HandleSetPlayerExp;
        EventBus<UISetPlayerHpEvent>.action += HandleSetPlayerHp;
        EventBus<UISetPlayerLifeEvent>.action += HandleSetPlayerlife;
        EventBus<UISetPlayerSkillSlotsEvent>.action += HandleSetPlayerSkillSlots;
        EventBus<UISetPlayerHealItemEvent>.action += HandleSetPlayerHealItem;
        EventBus<UIResetEvent>.action += HandleReset;
    }

    private void UnsubscribeEvents()
    {
        EventBus<UISetPlayerLevelEvent>.action -= HandleSetPlayerLevel;
        EventBus<UISetPlayerExpEvent>.action -= HandleSetPlayerExp;
        EventBus<UISetPlayerHpEvent>.action -= HandleSetPlayerHp;
        EventBus<UISetPlayerLifeEvent>.action -= HandleSetPlayerlife;
        EventBus<UISetPlayerSkillSlotsEvent>.action -= HandleSetPlayerSkillSlots;
        EventBus<UISetPlayerHealItemEvent>.action -= HandleSetPlayerHealItem;
        EventBus<UIResetEvent>.action -= HandleReset;
    }

    private void HandleSetPlayerLevel(UISetPlayerLevelEvent eventData)
    {
        currentLevel = Mathf.Max(1, eventData.Level);
        RefreshLevel();
    }

    private void HandleSetPlayerExp(UISetPlayerExpEvent eventData)
    {
        currentExp = Mathf.Max(0f, eventData.CurrentExp);
        requiredExp = Mathf.Max(1f, eventData.RequiredExp);
        RefreshExp();
    }

    private void HandleSetPlayerHp(UISetPlayerHpEvent eventData)
    {
        maxHp = Mathf.Max(1f, eventData.MaxHp);
        currentHp = Mathf.Clamp(eventData.CurrentHp, 0f, maxHp);
        RefreshHp();
    }

    private void HandleSetPlayerlife(UISetPlayerLifeEvent eventData)
    {
        maxLife = Mathf.Max(0, eventData.MaxLife);
        currentLife = Mathf.Clamp(eventData.CurrentLife, 0, maxLife);
        RefreshLife();
    }

    private void HandleSetPlayerSkillSlots(UISetPlayerSkillSlotsEvent eventData)
    {
        RefreshSkillSlots(eventData.SkillSlots);
    }

    private void HandleSetPlayerHealItem(UISetPlayerHealItemEvent eventData)
    {
        maxHealItemCount = Mathf.Max(0, eventData.MaxCount);
        healItemCount = Mathf.Clamp(eventData.Count, 0, maxHealItemCount);
        RefreshHealItem();
    }

    private void HandleReset(UIResetEvent eventData)
    {
        ResetHudView();
    }

    private void RefreshAll()
    {
        RefreshLevel();
        RefreshExp();
        RefreshHp();
        RefreshLife();
        RefreshHealItem();
    }

    private void RefreshLevel()
    {
        SetText(levelText, $"{currentLevel}Lv");
    }

    private void RefreshExp()
    {
        float ratio = Mathf.Clamp01(currentExp / requiredExp);

        if (expFillImage != null)
        {
            expFillImage.fillAmount = ratio;
        }

        SetText(expText, $"{Mathf.FloorToInt(currentExp)} / {Mathf.FloorToInt(requiredExp)}");
    }

    private void RefreshHp()
    {
        float ratio = Mathf.Clamp01(currentHp / maxHp);
    
        if (hpFillImage != null)
        {
            hpFillImage.fillAmount = ratio;
        }

        SetText(hpText, $"{Mathf.CeilToInt(currentHp)} / {Mathf.CeilToInt(maxHp)}");

        if (hpWarningObject != null)
        {
            hpWarningObject.SetActive(ratio <= hpWarningRatio);
        }
    }

    private void RefreshLife()
    {
        if (lifeIcons == null)
            return;

        for (int i = 0; i < lifeIcons.Length; i++)
        {
            if (lifeIcons[i] == null)
                continue;

            bool shouldShow = i < maxLife;
            bool isActiveLife = i < currentLife;

            lifeIcons[i].gameObject.SetActive(shouldShow);
            lifeIcons[i].color = isActiveLife ? Color.white : new Color(1f, 1f, 1f, 0.25f);
        }
    }

    private void RefreshSkillSlots(UIPlayerSkillSlotData[] skillSlots)
    {
        int slotCount = skillIconImages == null ? 0 : skillIconImages.Length;

        for (int i = 0; i < slotCount; i++)
        {
            bool hasData = skillSlots != null && i < skillSlots.Length;
            UIPlayerSkillSlotData slotData = hasData ? skillSlots[i] : default;

            RefreshSkillSlot(i, hasData, slotData);
        }
    }

    private void RefreshSkillSlot(int index, bool hasData, UIPlayerSkillSlotData slotData)
    {
        if (skillIconImages != null && index < skillIconImages.Length && skillIconImages[index] != null)
        {
            skillIconImages[index].sprite = hasData ? slotData.Icon : null;
            skillIconImages[index].enabled = hasData && slotData.Icon != null;
            skillIconImages[index].color = !hasData || slotData.IsAvailable ? Color.white : new Color(1f, 1f, 1f, 0.35f);
        }

        if (skillCooldownFillImages != null && index < skillCooldownFillImages.Length && skillCooldownFillImages[index] != null)
        {
            skillCooldownFillImages[index].fillAmount = hasData ? Mathf.Clamp01(slotData.CooldownProgress) : 0f;
        }

        if (skillKeyTexts != null && index < skillKeyTexts.Length)
        {
            SetText(skillKeyTexts[index], hasData ? slotData.KeyText : string.Empty);
        }

        if (skillLevelTexts != null && index < skillLevelTexts.Length)
        {
            SetText(skillLevelTexts[index], hasData ? $"Lv.{slotData.Level}" : string.Empty);
        }
    }

    private void RefreshHealItem()
    {
        if (healItemIconImage != null)
        {
            healItemIconImage.color = healItemCount > 0 ? Color.white : new Color(1f, 1f, 1f, 0.35f);
        }

        SetText(healItemCountText, $"{healItemCount}/{maxHealItemCount}");
    }

    private void SetText(Text targetText, string value)
    {
        if (targetText == null)
            return;

        targetText.text = value;
    }
}
