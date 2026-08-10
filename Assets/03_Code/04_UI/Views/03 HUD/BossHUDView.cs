using UnityEngine;
using UnityEngine.UI;

// 보스 이름 및 HP 상태 표시 HUD View
public class BossHUDView : UIViewBase
{
    [Header("Boss Info")]
    [SerializeField] private Text bossNameText;

    [Header("HP")]
    [SerializeField] private Image hpFillImage;

    private string currentBossName = "Boss";
    private float currentHp = 1f;
    private float maxHp = 1f;

    // 2026.08.10_UI 정리: 컴포넌트 초기화와 이벤트 구독을 준비한다.
    protected override void Awake()
    {
        base.Awake();
        SubscirbeEvents();
    }

    // 2026.08.10_UI 정리: 파괴 시 등록한 이벤트와 임시 UI 상태를 정리한다.
    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    // 2026.08.10_UI 정리: 화면 표시 시 필요한 UI 상태를 초기화한다.
    protected override void OnShow()
    {
        RefreshAll();
        EventBus<UIRequestBossHudDataEvent>.Publish(default);
    }

    // 2026.08.10_UI 정리: 화면 숨김 시 임시 UI 상태를 정리한다.
    // 2026.08.10_UI 정리: 보스 정보 표시 값을 반영한다.
    public void SetBossInfo(string bossName)
    {
        currentBossName = string.IsNullOrEmpty(bossName) ? "Boss" : bossName;
        RefreshBossName();
    }

    // 2026.08.10_UI 정리: 보스 체력 표시 값을 반영한다.
    public void SetBossHp(float currentHp, float maxHp)
    {
        this.maxHp = Mathf.Max(1f, maxHp);
        this.currentHp = Mathf.Clamp(currentHp, 0f, this.maxHp);

        RefreshHp();
    }

    // 2026.08.10_UI 정리: 보스 HUD 상태를 기본값으로 초기화한다.
    public void ResetBossHud()
    {
        currentBossName = "Boss";
        currentHp = 1f;
        maxHp = 1f;

        RefreshAll();
    }

    // 2026.08.10_UI 정리: 이 메서드의 UI 처리 역할을 수행한다.
    private void SubscirbeEvents()
    {
        EventBus<UISetBossHudDataEvent>.action += HandleSetBossHudData;
        EventBus<UIResetEvent>.action += HandleReset;
    }

    // 2026.08.10_UI 정리: 이벤트 이벤트 구독을 해제한다.
    private void UnsubscribeEvents()
    {
        EventBus<UISetBossHudDataEvent>.action -= HandleSetBossHudData;
        EventBus<UIResetEvent>.action -= HandleReset;
    }

    // 2026.08.10_UI 정리: Set 보스 HUD 데이터 관련 입력 또는 EventBus 요청을 처리한다.
    private void HandleSetBossHudData(UISetBossHudDataEvent eventData)
    {
        SetBossInfo(eventData.BossName);
        SetBossHp(eventData.CurrentHp, eventData.MaxHp);
    }

    // 2026.08.10_UI 정리: 초기화 관련 입력 또는 EventBus 요청을 처리한다.
    private void HandleReset(UIResetEvent eventData)
    {
        ResetBossHud();
    }

    // 2026.08.10_UI 정리: 현재 데이터로 전체 표시를 갱신한다.
    private void RefreshAll()
    {
        RefreshBossName();
        RefreshHp();
    }

    // 2026.08.10_UI 정리: 현재 데이터로 보스 Name 표시를 갱신한다.
    private void RefreshBossName()
    {
        if (bossNameText != null)
        {
            bossNameText.text = currentBossName;
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
}
