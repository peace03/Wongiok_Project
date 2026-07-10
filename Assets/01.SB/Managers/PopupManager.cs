using System;
using UnityEngine;

// 최상단 팝업을 열고 닫는 관리자입니다.
// PopupManager는 팝업 UI 표시와 UIManager의 Popup 상태 연동만 담당합니다.
// 저장, 씬 이동, 컷씬 종료 같은 실제 게임 로직은 ShowConfirm에 전달된 콜백에서 처리합니다.
public class PopupManager : MonoBehaviour, IInitializable
{
    public int Priority => (int)InitOrder.UI + 10;

    // 확인/취소 팝업 View입니다.
    // 실제 텍스트 표시와 버튼 클릭 이벤트 연결은 ConfirmPopupView가 담당합니다.
    [SerializeField] private ConfirmPopupView confirmPopupView;
    [SerializeField] private AlertPopupView alertPopupView;

    // 현재 Confirm 팝업에서 확인 버튼을 눌렀을 때 호출할 외부 콜백입니다.
    private Action currentConfirmCallback;

    // 현재 Confirm 팝업에서 취소 버튼을 눌렀을 때 호출할 외부 콜백입니다.
    private Action currentCancelCallback;

    // 현재 PopupManager가 어떤 팝업을 열어 둔 상태인지 기억합니다.
    // 지금 단계에서는 Confirm만 구현하지만, 이후 AlertPopupView가 추가될 수 있으므로 타입으로 관리합니다.
    private UIPopupType currentPopupType = UIPopupType.None;

    // 초기화 시 팝업을 모두 숨기고 이벤트 구독을 설정합니다.
    // Bootstrapper에서 초기화를 위한 Init구조
    public void Init()
    {
        HideConfirmPopupView();
        HideAlertPopupView();
        SetPopupState(UIPopupType.None);
        SubscribeEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    // 확인/취소 팝업을 표시합니다.
    // 이미 팝업이 열려 있다면 기존 팝업을 콜백 없이 닫고 새 팝업으로 교체합니다.
    public void ShowConfirm(
        string title,
        string message,
        Action onConfirm,
        Action onCancel = null,
        string confirmText = "확인",
        string cancelText = "취소")
    {
        if (confirmPopupView == null)
        {
            Debug.LogWarning("ConfirmPopupView가 연결되지 않았습니다.");
            return;
        }

        if (currentPopupType != UIPopupType.None)
        {
            CloseCurrentPopup();
        }

        currentConfirmCallback = onConfirm;
        currentCancelCallback = onCancel;
        currentPopupType = UIPopupType.Confirm;

        confirmPopupView.Setup(
            title,
            message,
            confirmText,
            cancelText,
            HandleConfirmClicked,
            HandleCancelClicked);

        confirmPopupView.Show();
        SetPopupState(UIPopupType.Confirm);
    }

    public void ShowAlert(
        string title,
        string message,
        Action onConfirm = null,
        string confirmText = "확인")
    {
        if (alertPopupView == null)
        {
            Debug.LogWarning("AlertPopupView가 연결되지 않았습니다.");
            return;
        }
        if (currentPopupType != UIPopupType.None)
        {
            CloseCurrentPopup();
        }

        currentConfirmCallback = onConfirm;
        currentPopupType = UIPopupType.Alert;

        alertPopupView.Setup(
            title,
            message,
            confirmText,
            HandleAlertConfirmClicked);

        alertPopupView.Show();
        SetPopupState(UIPopupType.Alert);
    }

    // 현재 열려 있는 팝업을 콜백 없이 닫습니다.
    // 새 팝업으로 교체하거나, 외부에서 강제로 팝업만 닫아야 할 때 사용합니다.
    public void CloseCurrentPopup()
    {
        if (currentPopupType == UIPopupType.None)
        {
            return;
        }

        HideCurrentPopupView();
        ClearCurrentCallbacks();

        currentPopupType = UIPopupType.None;
        SetPopupState(UIPopupType.None);
    }

    // 현재 팝업을 취소 처리합니다.
    // ESC로 팝업 닫기 또는 컷씬 스킵 취소 같은 상황에서 사용합니다.
    public void CancelCurrentPopup()
    {
        if (currentPopupType == UIPopupType.None)
        {
            return;
        }

        Action cancelCallback = currentCancelCallback;

        CloseCurrentPopup();
        cancelCallback?.Invoke();
    }

    private void SubscribeEvents()
    {
        EventBus<UIShowConfirmPopupEvent>.action += HandleShowConfirmPopup;
        EventBus<UIShowAlertPopupEvent>.action += HandleShowAlertPopup;
    }

    private void UnsubscribeEvents()
    {
        EventBus<UIShowConfirmPopupEvent>.action -= HandleShowConfirmPopup;
        EventBus<UIShowAlertPopupEvent>.action -= HandleShowAlertPopup;
    }

    private void HandleShowConfirmPopup(UIShowConfirmPopupEvent eventData)
    {
        ShowConfirm(
            eventData.Title,
            eventData.Message,
            eventData.OnConfirm,
            eventData.OnCancel,
            eventData.ConfirmText,
            eventData.CancelText);
    }
    
    private void HandleShowAlertPopup(UIShowAlertPopupEvent eventData)
    {
        ShowAlert(
            eventData.Title,
            eventData.Message,
            eventData.OnConfirm,
            eventData.ConfirmText);
    }

    // ConfirmPopupView의 확인 버튼에서 호출됩니다.
    // 팝업을 먼저 닫은 뒤 외부 확인 콜백을 실행해, 콜백 내부에서 화면 전환이 일어나도 팝업 상태가 남지 않게 합니다.
    private void HandleConfirmClicked()
    {
        Action confirmCallback = currentConfirmCallback;

        CloseCurrentPopup();
        confirmCallback?.Invoke();
    }

    private void HandleAlertConfirmClicked()
    {
        Action confirmCallback = currentConfirmCallback;

        CloseCurrentPopup();
        confirmCallback?.Invoke();
    }

    // ConfirmPopupView의 취소 버튼에서 호출됩니다.
    private void HandleCancelClicked()
    {
        CancelCurrentPopup();
    }

    // 현재 팝업 타입에 맞는 View를 숨깁니다.
    private void HideCurrentPopupView()
    {
        if (currentPopupType == UIPopupType.Confirm)
        {
            HideConfirmPopupView();
        }

        if (currentPopupType == UIPopupType.Alert)
        {
            HideAlertPopupView();
        }
    }

    // Confirm 팝업 View를 안전하게 숨깁니다.
    // UIViewBase.Hide()는 IsVisible이 false면 바로 종료하므로, 씬에서 처음부터 켜져 있던 경우까지 고려해 SetActive(false)를 보강합니다.
    private void HideConfirmPopupView()
    {
        if (confirmPopupView == null)
        {
            return;
        }

        confirmPopupView.Hide();

        if (!confirmPopupView.IsVisible)
        {
            confirmPopupView.gameObject.SetActive(false);
        }
    }

    private void HideAlertPopupView()
    {
        if (alertPopupView == null)
        {
            return;
        }

        alertPopupView.Hide();

        if (!alertPopupView.IsVisible)
        {
            alertPopupView.gameObject.SetActive(false);
        }
    }

    // 현재 팝업에 연결된 외부 콜백 참조를 정리합니다.
    private void ClearCurrentCallbacks()
    {
        currentConfirmCallback = null;
        currentCancelCallback = null;
    }

    // UIManager에 현재 팝업 상태를 알려 입력 정책이 갱신되게 합니다.
    // 현재는 직접 메서드 호출이지만, 팀 규칙상 EventBus 연결로 통일할 경우 EventBus<UISetPopupStateEvent>.Publish(...)를 사용하게 됩니다.
    private void SetPopupState(UIPopupType popupType)
    {
        EventBus<UISetPopupStateEvent>.Publish(new UISetPopupStateEvent(popupType));
    }
}
