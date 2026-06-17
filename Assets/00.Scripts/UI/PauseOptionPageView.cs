using UnityEngine;

public class PauseOptionPageView : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private CommonButtonView mainMenuButton;
    [SerializeField] private CommonButtonView quitGameButton;

    private void OnEnable()
    {
        SetupButtons();
    }

    private void OnDisable()
    {
        ClearButtons();
    }

    private void OnDestroy()
    {
        ClearButtons();
    }

    private void SetupButtons()
    {
        if (mainMenuButton != null)
            mainMenuButton.Setup("메인 메뉴", HandleMainMenuClicked);

        if (quitGameButton != null)
            quitGameButton.Setup("게임 종료", HandleQuitGameClicked);
    }

    private void ClearButtons()
    {
        if (mainMenuButton != null)
            mainMenuButton.Clear();

        if (quitGameButton != null)
            quitGameButton.Clear();
    }

    private void HandleMainMenuClicked()
    {
        EventBus<UIShowConfirmPopupEvent>.Publish(
            new UIShowConfirmPopupEvent(
                "메인 메뉴로 돌아가기",
                "현재 진행 상황을 끝내고 메인 메뉴로 돌아가?",
                 PublishMainMenuRequested));
    }

    private void HandleQuitGameClicked()
    {
        EventBus<UIShowConfirmPopupEvent>.Publish(
            new UIShowConfirmPopupEvent(
                "게임 종료",
                "게임 종료하실?",
                PublishQuitGameRequested));
    }

    private void PublishMainMenuRequested()
    {
        EventBus<UIPauseMainMenuRequestedEvent>.Publish(default);
    }

    private void PublishQuitGameRequested()
    {
        EventBus<UIPauseQuitGameRequestedEvent>.Publish(default);
    }
}
