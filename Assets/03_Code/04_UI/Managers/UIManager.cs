using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

// EventBus와 Bootstrapper에서의 초기화 진행을 위한 수정이 필요합니다.
// 현재 클래스는 Bootstrapper가 Init()을 호출하는 대상이며, 외부 시스템은 직접 메서드를 호출하기보다 UIEvent를 발행하는 방향으로 맞춰갈 예정입니다.

// UI의 현재 Screen, Overlay, Popup 상태를 관리하는 중심 클래스입니다.
// 이 클래스는 UI 표시와 입력 차단 정책만 담당하고, 저장/씬 로드/스킬 계산 같은 게임 로직은 직접 처리하지 않습니다.
public class UIManager : MonoBehaviour, IInitializable
{
    // Inspector에서 Screen 상태와 실제 View 오브젝트를 연결하기 위한 데이터입니다.
    // 예: UIScreenState.Title -> TitleView
    [Serializable]
    private struct ScreenBinding
    {
        public UIScreenState state;
        public UIViewBase view;
    }

    // Inspector에서 Overlay 상태와 실제 View 오브젝트를 연결하기 위한 데이터입니다.
    // 예: UIOverlayState.Pause -> PauseMenuView
    [Serializable]
    private struct OverlayBinding
    {
        public UIOverlayState state;
        public UIViewBase view;
    }

    // 서로 동시에 떠 있으면 안 되는 기본 화면 View 목록입니다.
    [Header("Screen Views")]
    [SerializeField] private List<ScreenBinding> screenBindings = new();

    // 기본 화면 위에 올라오는 차단형 UI View 목록입니다.
    [Header("Overlay Views")]
    [SerializeField] private List<OverlayBinding> overlayBindings = new();

    // 플레이 중 표시되는 HUD View입니다.
    // Boss HUD는 입력을 막지 않으므로 Overlay가 아니라 별도 HUD로 관리합니다.
    [Header("HUD Views")]
    [SerializeField] private UIViewBase playerHudView;
    [SerializeField] private UIViewBase bossHudView;

    // 런타임에서 빠르게 View를 찾기 위해 Inspector 리스트를 Dictionary로 변환해 둡니다.
    private readonly Dictionary<UIScreenState, UIViewBase> screenViews = new();
    private readonly Dictionary<UIOverlayState, UIViewBase> overlayViews = new();

    // 중복 초기화 방지용 플래그입니다.
    // 후속 구현에서는 Init() 끝에서 true로 바꾸고, ResetUI()와는 별개로 "초기화가 이미 끝났는지"만 판단하게 사용합니다.
    private bool isInitialized;

    // Bootstrapper가 IInitializable 구현체들을 정렬할 때 사용하는 초기화 우선순위입니다.
    // UI는 Player/Skill/Mob/Boss 이후에 초기화되도록 InitOrder.UI 값을 사용합니다.
    public int Priority => (int)InitOrder.UI;

    // 현재 표시 중인 기본 화면 상태입니다.
    public UIScreenState CurrentScreenState { get; private set; } = UIScreenState.None;

    // 현재 열려 있는 차단형 Overlay 상태입니다.
    public UIOverlayState CurrentOverlayState { get; private set; } = UIOverlayState.None;

    // 현재 열려 있는 최상단 Popup 종류입니다.
    // 실제 Popup View 표시는 추후 PopupManager가 담당하고, UIManager는 입력 정책에 필요한 상태만 기억합니다.
    public UIPopupType CurrentPopupType { get; private set; } = UIPopupType.None;

    // 외부 입력 시스템에서 "현재 플레이어 조작을 막아야 하는가"를 간단히 확인할 때 사용하는 값입니다.
    public bool IsGameplayInputBlocked => GetInputBlockType() != UIInputBlockType.None;

    // Bootstrapper에서 호출되는 UIManager의 단일 초기화 진입점입니다.
    // 여기서는 Inspector에 연결된 View 목록을 런타임 Dictionary로 준비합니다.
    // 후속 구현에서는 SubscribeEvents(), HideAllViews(), ResetUI(), isInitialized = true 처리가 이 흐름에 들어가야 합니다.
    public void Init()
    {
        if (isInitialized)
            return;

        InitializeScreenViews();
        InitializeOverlayViews();
        HideAllViews();
        SubscribeEvents();
        ResetUI();

        isInitialized = true;

    }

    // Unity 오브젝트가 파괴될 때 호출되는 정리 지점입니다.
    // EventBus는 static 이벤트를 사용하므로, 구독을 시작했다면 여기서 반드시 UnsubscribeEvents()를 호출해야 합니다.
    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    // UI 전체 상태를 한 번에 초기화하는 Reset 진입점입니다.
    // Single Entry Point Reset 구조에서는 UIResetEvent를 받은 뒤 이 메서드에서 Screen/Overlay/Popup/HUD/TimeScale을 초기 상태로 돌리는 역할을 맡게 됩니다.
    public void ResetUI()
    {
        CloseCurrentOverlay();
        SetPopupState(UIPopupType.None);
        HideAllViews();

        CurrentScreenState = UIScreenState.None;
        CurrentOverlayState = UIOverlayState.None;
        Time.timeScale = 1f;

        ChangeScreen(UIScreenState.Title);
    }

    // UI 관련 EventBus 이벤트를 UIManager의 실제 처리 메서드에 연결합니다.
    // 외부 시스템은 UIManager를 직접 참조하지 않고 EventBus<T>.Publish(...)로 요청을 보내는 방식으로 맞춥니다.
    private void SubscribeEvents()
    {
        EventBus<UIChangeScreenEvent>.action += HandleChangeScreen;
        EventBus<UIOpenOverlayEvent>.action += HandleOpenOverlay;
        EventBus<UICloseOverlayEvent>.action += HandleCloseOverlay;
        EventBus<UISetPopupStateEvent>.action += HandleSetPopupState;
        EventBus<UISetBossHudVisibleEvent>.action += HandleSetBossHudVisible;
        EventBus<UIResetEvent>.action += HandleReset;

    }

    // SubscribeEvents()에서 연결한 EventBus 구독을 해제합니다.
    // static 이벤트는 구독자가 파괴된 뒤에도 참조를 잡을 수 있으므로, OnDestroy()에서 호출하는 것이 안전합니다.
    private void UnsubscribeEvents()
    {
        EventBus<UIChangeScreenEvent>.action -= HandleChangeScreen;
        EventBus<UIOpenOverlayEvent>.action -= HandleOpenOverlay;
        EventBus<UICloseOverlayEvent>.action -= HandleCloseOverlay;
        EventBus<UISetPopupStateEvent>.action -= HandleSetPopupState;
        EventBus<UISetBossHudVisibleEvent>.action -= HandleSetBossHudVisible;
        EventBus<UIResetEvent>.action -= HandleReset;
    }

    // UIChangeScreenEvent를 실제 화면 전환 메서드로 중계합니다.
    // 이벤트 발행자는 "어떤 화면으로 바꿀지"만 전달하고, View Show/Hide 세부 처리는 UIManager가 담당합니다.
    private void HandleChangeScreen(UIChangeScreenEvent eventData)
    {
        ChangeScreen(eventData.ScreenState);
    }

    // UIOpenOverlayEvent를 실제 Overlay 열기 메서드로 중계합니다.
    // Cutscene, LevelUp, Pause처럼 게임플레이 입력을 막는 UI를 열 때 사용합니다.
    private void HandleOpenOverlay(UIOpenOverlayEvent eventData)
    {
        OpenOverlay(eventData.OverlayState);
    }

    // UICloseOverlayEvent를 현재 Overlay 닫기 메서드로 중계합니다.
    // eventData에 OverlayState가 들어 있지만, 현재 구현은 "현재 열려 있는 Overlay"를 닫는 정책입니다.
    private void HandleCloseOverlay(UICloseOverlayEvent eventData)
    {
        CloseCurrentOverlay();
    }

    // UISetPopupStateEvent를 Popup 상태 기록 메서드로 중계합니다.
    // Popup View 생성/닫기는 PopupManager가 담당하고, UIManager는 입력 차단 정책에 필요한 상태만 기억합니다.
    private void HandleSetPopupState(UISetPopupStateEvent eventData)
    {
        SetPopupState(eventData.PopupType);
    }

    // UISetBossHudVisibleEvent를 Boss HUD 표시/숨김 메서드로 중계합니다.
    // Boss HUD는 Overlay가 아니므로 입력 차단이나 TimeScale 변경 없이 표시 상태만 바꿉니다.
    private void HandleSetBossHudVisible(UISetBossHudVisibleEvent eventData)
    {
        SetBossHudVisible(eventData.IsVisible);
    }

    // UIResetEvent를 UI 전체 초기화 메서드로 중계합니다.
    // 게임 전체 Reset의 Single Entry Point에서 이 이벤트를 발행하면 UI는 ResetUI()를 통해 자기 상태만 정리합니다.
    private void HandleReset(UIResetEvent eventData)
    {
        ResetUI();
    }

    // 기본 화면을 전환합니다.
    // Screen이 바뀔 때는 기존 Overlay를 닫아서 이전 화면의 일시정지/컷씬 UI가 남지 않게 합니다.
    public void ChangeScreen(UIScreenState nextState)
    {
        if (CurrentScreenState == nextState)
        {
            return;
        }

        CloseCurrentOverlay();

        HideScreen(CurrentScreenState);
        CurrentScreenState = nextState;
        ShowScreen(CurrentScreenState);

        ApplyHudPolicy();
        ApplyInputPolicy();
    }

    // 차단형 Overlay를 엽니다.
    // 동시에 하나의 Overlay만 허용하므로, 이미 다른 Overlay가 열려 있으면 먼저 닫습니다.
    public void OpenOverlay(UIOverlayState overlayState)
    {
        if (overlayState == UIOverlayState.None)
        {
            return;
        }

        if (CurrentOverlayState == overlayState)
        {
            return;
        }

        if (CurrentOverlayState != UIOverlayState.None)
        {
            CloseCurrentOverlay();
        }

        CurrentOverlayState = overlayState;
        ShowOverlay(CurrentOverlayState);

        ApplyHudPolicy();
        ApplyInputPolicy();
    }

    // 현재 열려 있는 Overlay를 닫습니다.
    // 닫은 뒤 HUD와 입력 정책을 현재 Screen 상태에 맞게 다시 적용합니다.
    public void CloseCurrentOverlay()
    {
        if (CurrentOverlayState == UIOverlayState.None)
        {
            return;
        }

        HideOverlay(CurrentOverlayState);
        CurrentOverlayState = UIOverlayState.None;

        ApplyHudPolicy();
        ApplyInputPolicy();
    }

    // Popup 상태를 기록합니다.
    // 실제 Alert/Confirm View 생성과 버튼 콜백 처리는 추후 PopupManager에서 담당합니다.
    public void SetPopupState(UIPopupType popupType)
    {
        CurrentPopupType = popupType;
        ApplyInputPolicy();
    }

    // Boss HUD 표시 여부를 외부에서 제어할 수 있게 열어 둔 메서드입니다.
    // Boss HUD는 입력 차단이나 TimeScale 변경을 하지 않는 단순 표시 UI입니다.
    public void SetBossHudVisible(bool isVisible)
    {
        if (bossHudView == null)
        {
            return;
        }

        if (isVisible)
        {
            bossHudView.Show();
            return;
        }

        bossHudView.Hide();
    }

    // 현재 UI 상태를 기준으로 입력 차단 수준을 계산합니다.
    // 우선순위는 Popup > Loading > Overlay > Screen > Game Input 순서입니다.
    public UIInputBlockType GetInputBlockType()
    {
        if (CurrentPopupType != UIPopupType.None)
        {
            return UIInputBlockType.GameplayOnly;
        }

        if (CurrentScreenState == UIScreenState.Loading)
        {
            return UIInputBlockType.All;
        }

        if (CurrentOverlayState == UIOverlayState.Cutscene)
        {
            return UIInputBlockType.GameplayOnly;
        }

        if (CurrentOverlayState == UIOverlayState.LevelUp)
        {
            return UIInputBlockType.GameplayOnly;
        }

        if (CurrentOverlayState == UIOverlayState.Pause)
        {
            return UIInputBlockType.GameplayOnly;
        }

        if (CurrentScreenState != UIScreenState.InGame)
        {
            return UIInputBlockType.GameplayOnly;
        }

        return UIInputBlockType.None;
    }

    // Inspector에서 연결한 Screen Binding 목록을 Dictionary로 변환합니다.
    // None 상태나 View가 비어 있는 항목은 무시합니다.
    private void InitializeScreenViews()
    {
        screenViews.Clear();

        foreach (ScreenBinding binding in screenBindings)
        {
            if (binding.state == UIScreenState.None || binding.view == null)
            {
                continue;
            }

            if (screenViews.ContainsKey(binding.state))
            {
                Debug.LogWarning($"Duplicate screen binding found: {binding.state}");
                continue;
            }

            screenViews.Add(binding.state, binding.view);
        }
    }

    // Inspector에서 연결한 Overlay Binding 목록을 Dictionary로 변환합니다.
    // 같은 상태가 중복으로 등록되면 첫 번째 항목만 사용하고 경고를 남깁니다.
    private void InitializeOverlayViews()
    {
        overlayViews.Clear();

        foreach (OverlayBinding binding in overlayBindings)
        {
            if (binding.state == UIOverlayState.None || binding.view == null)
            {
                continue;
            }

            if (overlayViews.ContainsKey(binding.state))
            {
                Debug.LogWarning($"Duplicate overlay binding found: {binding.state}");
                continue;
            }

            overlayViews.Add(binding.state, binding.view);
        }
    }

    // 시작 시 모든 Screen, Overlay, HUD를 숨깁니다.
    // 이후 Start에서 ChangeScreen(Title)을 호출해 타이틀만 다시 표시합니다.
    private void HideAllViews()
    {
        foreach (UIViewBase view in screenViews.Values)
        {
            HideView(view);
        }

        foreach (UIViewBase view in overlayViews.Values)
        {
            HideView(view);
        }

        HideView(playerHudView);
        HideView(bossHudView);
    }

    // 지정한 Screen View를 표시합니다.
    // 아직 Binding이 연결되지 않은 상태는 조용히 무시합니다.
    private void ShowScreen(UIScreenState state)
    {
        if (!screenViews.TryGetValue(state, out UIViewBase view))
        {
            return;
        }

        view.Show();
    }

    // 지정한 Screen View를 숨깁니다.
    private void HideScreen(UIScreenState state)
    {
        if (!screenViews.TryGetValue(state, out UIViewBase view))
        {
            return;
        }

        HideView(view);
    }

    // 지정한 Overlay View를 표시합니다.
    private void ShowOverlay(UIOverlayState state)
    {
        if (!overlayViews.TryGetValue(state, out UIViewBase view))
        {
            return;
        }

        view.Show();
    }

    // 지정한 Overlay View를 숨깁니다.
    private void HideOverlay(UIOverlayState state)
    {
        if (!overlayViews.TryGetValue(state, out UIViewBase view))
        {
            return;
        }

        HideView(view);
    }

    // View 숨김을 안전하게 처리하는 공통 함수입니다.
    // UIViewBase.Hide()는 IsVisible이 false면 바로 종료하므로, 씬에서 처음부터 켜져 있던 오브젝트까지 확실히 끄기 위해 SetActive(false)를 보강합니다.
    private void HideView(UIViewBase view)
    {
        if (view == null)
        {
            return;
        }

        view.Hide();

        // 이미 켜져있어 IsVisible이 false인 경우, 동작하지 않아 예외처리로 보강
        if (!view.IsVisible)
        {
            view.gameObject.SetActive(false);
        }
    }

    // 현재 Screen/Overlay 상태에 맞게 Player HUD 표시와 dim 상태를 적용합니다.
    // Boss HUD는 전투 시스템 쪽에서 직접 SetBossHudVisible로 켜고 끄는 구조입니다.
    private void ApplyHudPolicy()
    {
        bool shouldShowPlayerHud =
            CurrentScreenState == UIScreenState.InGame ||
            CurrentOverlayState == UIOverlayState.Cutscene ||
            CurrentOverlayState == UIOverlayState.LevelUp ||
            CurrentOverlayState == UIOverlayState.Pause;

        if (playerHudView != null)
        {
            if (shouldShowPlayerHud)
            {
                playerHudView.Show();
            }
            else
            {
                HideView(playerHudView);
            }
        }

        if (playerHudView != null)
        {
            bool shouldDimHud =
                CurrentOverlayState == UIOverlayState.Cutscene ||
                CurrentOverlayState == UIOverlayState.LevelUp;

            playerHudView.SetDimmed(shouldDimHud);
        }
    }

    // 현재 Overlay 상태에 맞게 게임 시간을 멈추거나 복구합니다.
    // 지금은 UIManager가 직접 Time.timeScale을 제어하지만, 추후 시간 제어가 복잡해지면 별도 서비스로 분리할 수 있습니다.
    private void ApplyInputPolicy()
    {
        ApplyTimeScalePolicy();
    }

    // 임시 TimeScale
    private void ApplyTimeScalePolicy()
    {
        bool shouldPauseGameplay = ShouldPauseGameplayByUIState();
        Time.timeScale = shouldPauseGameplay ? 0f : 1f;

        // 만약 이벤트 버스로 사용한다면
        // EventBus<>.Publish();

    }

    private bool ShouldPauseGameplayByUIState()
    {
        if (CurrentPopupType != UIPopupType.None)
            return true;

        if (CurrentOverlayState == UIOverlayState.Cutscene)
            return true;

        if (CurrentOverlayState == UIOverlayState.LevelUp)
            return true;

        if (CurrentOverlayState == UIOverlayState.Pause)
            return true;

        return false;
    }

}

