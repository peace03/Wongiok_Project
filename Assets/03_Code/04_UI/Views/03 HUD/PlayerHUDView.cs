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

    [Header("HP")]
    [SerializeField] private Image hpFillImage;

    [Header("Life")]
    [SerializeField] private Image[] lifeIcons;

    [Header("Skill Slots")]
    [SerializeField] private Image[] skillIconImages;
    [SerializeField] private Image[] skillCooldownFillImages;
    [SerializeField] private Text[] skillKeyTexts;
    [SerializeField] private Text[] skillLevelTexts;
    [SerializeField] private Image[] skillReadyFlashImages;
    [SerializeField] private float readyFlashDuration = 0.35f;
    [SerializeField] private float readyFlashMaxAlpha = 0.8f;

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

    private bool[] previousSkillAvailable;
    private bool[] previousSkillInitialized;
    private float[] readyFlashTimers;

    // 2026.08.10_UI 정리: 컴포넌트 초기화와 이벤트 구독을 준비한다.
    protected override void Awake()
    {
        base.Awake();
        SubscribeEvents();
    }

    // 2026.08.10_UI 정리: 파괴 시 등록한 이벤트와 임시 UI 상태를 정리한다.
    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    // 2026.08.10_UI 정리: 프레임 단위 UI 상태와 입력을 갱신한다.
    private void Update()
    {
        UpdateReadyFlash();
    }

    // 2026.08.10_UI 정리: 화면 표시 시 필요한 UI 상태를 초기화한다.
    protected override void OnShow()
    {
        RefreshAll();
    }

    // 2026.08.10_UI 정리: 화면 숨김 시 임시 UI 상태를 정리한다.
    protected override void OnHide()
    {
        ClearReadyFlashState();
    }

    // 2026.08.10_UI 정리: HUD View 상태를 기본값으로 초기화한다.
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

        ClearReadyFlashState();
        RefreshAll();
    }

    // 2026.08.10_UI 정리: 이벤트 이벤트를 구독한다.
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

    // 2026.08.10_UI 정리: 이벤트 이벤트 구독을 해제한다.
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

    // 2026.08.10_UI 정리: Set 플레이어 레벨 관련 입력 또는 EventBus 요청을 처리한다.
    private void HandleSetPlayerLevel(UISetPlayerLevelEvent eventData)
    {
        currentLevel = Mathf.Max(1, eventData.Level);
        RefreshLevel();
    }

    // 2026.08.10_UI 정리: Set 플레이어 Exp 관련 입력 또는 EventBus 요청을 처리한다.
    private void HandleSetPlayerExp(UISetPlayerExpEvent eventData)
    {
        currentExp = Mathf.Max(0f, eventData.CurrentExp);
        requiredExp = Mathf.Max(1f, eventData.RequiredExp);
        RefreshExp();
    }

    // 2026.08.10_UI 정리: Set 플레이어 체력 관련 입력 또는 EventBus 요청을 처리한다.
    private void HandleSetPlayerHp(UISetPlayerHpEvent eventData)
    {
        maxHp = Mathf.Max(1f, eventData.MaxHp);
        currentHp = Mathf.Clamp(eventData.CurrentHp, 0f, maxHp);
        RefreshHp();
    }

    // 2026.08.10_UI 정리: Set Playerlife 관련 입력 또는 EventBus 요청을 처리한다.
    private void HandleSetPlayerlife(UISetPlayerLifeEvent eventData)
    {
        maxLife = Mathf.Max(0, eventData.MaxLife);
        currentLife = Mathf.Clamp(eventData.CurrentLife, 0, maxLife);
        RefreshLife();
    }

    // 2026.08.10_UI 정리: Set 플레이어 스킬 슬롯 관련 입력 또는 EventBus 요청을 처리한다.
    private void HandleSetPlayerSkillSlots(UISetPlayerSkillSlotsEvent eventData)
    {
        RefreshSkillSlots(eventData.SkillSlots);
    }

    // 2026.08.10_UI 정리: Set 플레이어 회복 아이템 관련 입력 또는 EventBus 요청을 처리한다.
    private void HandleSetPlayerHealItem(UISetPlayerHealItemEvent eventData)
    {
        maxHealItemCount = Mathf.Max(0, eventData.MaxCount);
        healItemCount = Mathf.Clamp(eventData.Count, 0, maxHealItemCount);
        RefreshHealItem();
    }

    // 2026.08.10_UI 정리: 초기화 관련 입력 또는 EventBus 요청을 처리한다.
    private void HandleReset(UIResetEvent eventData)
    {
        ResetHudView();
    }

    // 2026.08.10_UI 정리: 현재 데이터로 전체 표시를 갱신한다.
    private void RefreshAll()
    {
        RefreshLevel();
        RefreshExp();
        RefreshHp();
        RefreshLife();
        RefreshHealItem();
    }

    // 2026.08.10_UI 정리: 현재 데이터로 레벨 표시를 갱신한다.
    private void RefreshLevel()
    {
        SetText(levelText, $"{currentLevel}Lv");
    }

    // 2026.08.10_UI 정리: 현재 데이터로 Exp 표시를 갱신한다.
    private void RefreshExp()
    {
        float ratio = Mathf.Clamp01(currentExp / requiredExp);

        if (expFillImage != null)
        {
            expFillImage.fillAmount = ratio;
        }

    }

    // 2026.08.10_UI 정리: 현재 데이터로 체력 표시를 갱신한다.
    private void RefreshHp()
    {
        float ratio = Mathf.Clamp01(currentHp / maxHp);
    
        if (hpFillImage != null)
        {
            hpFillImage.fillAmount = ratio;
        }

    }

    // 2026.08.10_UI 정리: 현재 데이터로 목숨 표시를 갱신한다.
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

    // 2026.08.10_UI 정리: 현재 데이터로 스킬 슬롯 표시를 갱신한다.
    private void RefreshSkillSlots(UIPlayerSkillSlotData[] skillSlots)
    {
        int slotCount = skillIconImages == null ? 0 : skillIconImages.Length;

        EnsureSkillReadyState(slotCount);

        for (int i = 0; i < slotCount; i++)
        {
            bool hasData = skillSlots != null && i < skillSlots.Length;
            UIPlayerSkillSlotData slotData = hasData ? skillSlots[i] : default;

            RefreshSkillSlot(i, hasData, slotData);
        }
    }

    // 2026.08.10_UI 정리: 현재 데이터로 스킬 슬롯 표시를 갱신한다.
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

        bool hasUsableSkill = hasData && slotData.Icon != null && slotData.Level > 0;
        bool isAvailable = hasUsableSkill && slotData.IsAvailable;

        if (previousSkillInitialized != null && index < previousSkillInitialized.Length)
        {
            if (previousSkillInitialized[index] && !previousSkillAvailable[index] && isAvailable)
            {
                TriggerReadyFlash(index);
            }

            previousSkillAvailable[index] = isAvailable;
            previousSkillInitialized[index] = true;
        }
    }

    // 2026.08.10_UI 정리: 현재 데이터로 회복 아이템 표시를 갱신한다.
    private void RefreshHealItem()
    {
        if (healItemIconImage != null)
        {
            healItemIconImage.color = healItemCount > 0 ? Color.white : new Color(1f, 1f, 1f, 0.35f);
        }

        SetText(healItemCountText, $"{healItemCount}/{maxHealItemCount}");
    }

    // 2026.08.10_UI 정리: 스킬 준비 상태 처리 상태가 준비되었는지 보장한다.
    private void EnsureSkillReadyState(int slotCount)
    {
        if (previousSkillAvailable != null && previousSkillAvailable.Length == slotCount) return;

        previousSkillAvailable = new bool[slotCount];
        previousSkillInitialized = new bool[slotCount];
        readyFlashTimers = new float[slotCount];

        ClearReadyFlashImage();
    }

    // 2026.08.10_UI 정리: 준비 Flash UI 강조 연출을 시작한다.
    private void TriggerReadyFlash(int index)
    {
        if (readyFlashTimers == null || index >= readyFlashTimers.Length) return;

        readyFlashTimers[index] = readyFlashDuration;

        if (skillReadyFlashImages != null &&
            index < skillReadyFlashImages.Length &&
            skillReadyFlashImages[index] != null)
        {
            SetImageAlpha(skillReadyFlashImages[index], readyFlashMaxAlpha);
        }
    }

    // 2026.08.10_UI 정리: 이 메서드의 UI 처리 역할을 수행한다.
    private void UpdateReadyFlash()
    {
        if (readyFlashTimers == null) return;

        bool hasChanged = false;

        for (int i = 0; i < readyFlashTimers.Length; i++)
        {
            if (readyFlashTimers[i] <= 0f) continue;

            readyFlashTimers[i] = Mathf.Max(0f, readyFlashTimers[i] - Time.unscaledDeltaTime);

            float ratio = readyFlashDuration <= 0f ? 0f : readyFlashTimers[i] / readyFlashDuration;

            if (skillReadyFlashImages != null &&
                i < skillReadyFlashImages.Length &&
                skillReadyFlashImages[i] != null)
            {
                SetImageAlpha(skillReadyFlashImages[i], readyFlashMaxAlpha * ratio);
            }

            hasChanged = true;
        }

        if (!hasChanged) return;
    }

    // 2026.08.10_UI 정리: 준비 Flash 상태 상태를 정리한다.
    private void ClearReadyFlashState()
    {
        if (readyFlashTimers != null)
        {
            for (int i = 0; i < readyFlashTimers.Length; i++)
                readyFlashTimers[i] = 0f;
        }

        ClearReadyFlashImage();
    }

    // 2026.08.10_UI 정리: 준비 Flash 이미지 상태를 정리한다.
    private void ClearReadyFlashImage()
    {
        if (skillReadyFlashImages == null) return;

        for (int i = 0; i < skillReadyFlashImages.Length; i++)
        {
            if (skillReadyFlashImages[i] == null) continue;

            SetImageAlpha(skillReadyFlashImages[i], 0f);
        }
    }

    // 2026.08.10_UI 정리: 이미지 Alpha 표시 값을 반영한다.
    private void SetImageAlpha(Image targetImage, float alpha)
    {
        if (targetImage == null)
            return;

        Color color = targetImage.color;
        color.a = alpha;
        targetImage.color = color;
    }

    // 2026.08.10_UI 정리: 텍스트 표시 값을 반영한다.
    private void SetText(Text targetText, string value)
    {
        if (targetText == null)
            return;

        targetText.text = value;
    }
}
