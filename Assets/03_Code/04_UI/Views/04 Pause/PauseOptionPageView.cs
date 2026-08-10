using UnityEngine;

public class PauseOptionPageView : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private CommonButtonView mainMenuButton;
    [SerializeField] private CommonButtonView quitGameButton;

    // 2026.08.10_UI 정리: 활성화 시 필요한 UI 상태와 이벤트 구독을 준비한다.
    private void OnEnable()
    {
        SetupButtons();
    }

    // 2026.08.10_UI 정리: 비활성화 시 등록한 이벤트와 임시 UI 상태를 정리한다.
    private void OnDisable()
    {
        ClearButtons();
    }

    // 2026.08.10_UI 정리: 파괴 시 등록한 이벤트와 임시 UI 상태를 정리한다.
    private void OnDestroy()
    {
        ClearButtons();
    }

    // 2026.08.10_UI 정리: 전달받은 데이터와 콜백으로 Buttons 상태를 설정한다.
    private void SetupButtons()
    {
        if (mainMenuButton != null)
            mainMenuButton.Setup(UITextManager.Get("Pause.MainMenu"), HandleMainMenuClicked);

        if (quitGameButton != null)
            quitGameButton.Setup(UITextManager.Get("Pause.Quit"), HandleQuitGameClicked);
    }

    // 2026.08.10_UI 정리: Buttons 상태를 정리한다.
    private void ClearButtons()
    {
        if (mainMenuButton != null)
            mainMenuButton.Clear();

        if (quitGameButton != null)
            quitGameButton.Clear();
    }

    // 2026.08.10_UI 정리: 메인 메뉴 클릭 관련 입력 또는 EventBus 요청을 처리한다.
    private void HandleMainMenuClicked()
    {
        EventBus<UIShowConfirmPopupEvent>.Publish(
            new UIShowConfirmPopupEvent(
                UITextManager.Get("Pause.MainMenuConfirmTitle"),
                UITextManager.Get("Pause.MainMenuConfirmMessage"),
                 PublishMainMenuRequested));
    }

    // 2026.08.10_UI 정리: 종료 게임 클릭 관련 입력 또는 EventBus 요청을 처리한다.
    private void HandleQuitGameClicked()
    {
        EventBus<UIShowConfirmPopupEvent>.Publish(
            new UIShowConfirmPopupEvent(
                UITextManager.Get("Pause.QuitConfirmTitle"),
                UITextManager.Get("Pause.QuitConfirmMessage"),
                PublishQuitGameRequested));
    }

    // 2026.08.10_UI 정리: 메인 메뉴 Requested 요청 또는 상태를 EventBus로 발행한다.
    private void PublishMainMenuRequested()
    {
        EventBus<UIPauseMainMenuRequestedEvent>.Publish(
            new UIPauseMainMenuRequestedEvent());
    }

    // 2026.08.10_UI 정리: 종료 게임 Requested 요청 또는 상태를 EventBus로 발행한다.
    private void PublishQuitGameRequested()
    {
        EventBus<UIPauseQuitGameRequestedEvent>.Publish(default);
    }
}
