using System;

// UI 팝업 상태 이벤트입니다.
// PopupManager가 팝업을 열거나 닫을 때 UIManager의 입력 차단 정책을 갱신하기 위해 발행하는 용도입니다.
public struct UISetPopupStateEvent
{
    // 현재 최상단에 떠 있는 Popup 종류입니다.
    // 팝업이 닫힌 상태라면 UIPopupType.None을 사용합니다.
    public UIPopupType PopupType { get; private set; }

    // 이벤트 발행 시 현재 Popup 타입을 함께 넘깁니다.
    public UISetPopupStateEvent(UIPopupType popupType)
    {
        PopupType = popupType;
    }
}

// UI 확인 팝업 표시 이벤트입니다.
public struct UIShowConfirmPopupEvent
{
    public string Title { get; private set; }
    public string Message { get; private set; }
    public string ConfirmText { get; private set; }
    public string CancelText { get; private set; }
    public Action OnConfirm { get; private set; }
    public Action OnCancel { get; private set; }

    // 2026.08.10_UI 정리: UI 이벤트 또는 표시 데이터를 초기화한다.
    public UIShowConfirmPopupEvent(
        string title,
        string message,
        Action onConfirm,
        Action onCancel = null,
        string confirmText = null,
        string cancelText = null)
    {
        Title = title;
        Message = message;
        OnConfirm = onConfirm;
        OnCancel = onCancel;
        ConfirmText = confirmText;
        CancelText = cancelText;
    }
}

public struct UIShowAlertPopupEvent
{
    public string Title { get; private set; }
    public string Message { get; private set; }
    public string ConfirmText { get; private set; }
    public Action OnConfirm { get; private set; }

    // 2026.08.10_UI 정리: UI 이벤트 또는 표시 데이터를 초기화한다.
    public UIShowAlertPopupEvent(
        string title,
        string message,
        Action onConfirm = null,
        string confirmText = null)
    {
        Title = title;
        Message = message;
        OnConfirm = onConfirm;
        ConfirmText = confirmText;
    }
}
