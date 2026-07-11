using System;
using UnityEngine;
using UnityEngine.UI;

// 확인/취소 버튼을 가진 공통 팝업 View입니다.
// 이 View는 텍스트와 버튼 표시만 담당하고, 실제 게임 로직은 외부에서 전달받은 콜백으로 넘깁니다.
public class ConfirmPopupView : UIViewBase
{
    // 팝업 제목 텍스트입니다.
    // 예: "컷씬 건너뛰기", "게임 종료"
    [SerializeField] private Text titleText;

    // 팝업 본문 텍스트입니다.
    // 컷씬 스킵 확인창에서는 해당 영상의 간략한 줄거리가 들어갑니다.
    [SerializeField] private Text messageText;

    // 확인 버튼입니다.
    [SerializeField] private Button confirmButton;

    // 취소 버튼입니다.
    [SerializeField] private Button cancelButton;

    // 확인 버튼 안에 표시되는 문구입니다.
    [SerializeField] private Text confirmButtonLabelText;

    // 취소 버튼 안에 표시되는 문구입니다.
    [SerializeField] private Text cancelButtonLabelText;

    // 확인 버튼 클릭 시 실행할 콜백입니다.
    private Action confirmCallback;

    // 취소 버튼 클릭 시 실행할 콜백입니다.
    private Action cancelCallback;

    // 팝업에 표시할 데이터와 버튼 동작을 주입합니다.
    // View는 이 값을 표시하고 버튼 클릭 시 콜백만 호출합니다.
    public void Setup(
        string title,
        string message,
        string confirmText,
        string cancelText,
        Action onConfirm,
        Action onCancel)
    {
        SetText(titleText, title);
        SetText(messageText, message);
        SetText(confirmButtonLabelText, confirmText);
        SetText(cancelButtonLabelText, cancelText);

        confirmCallback = onConfirm;
        cancelCallback = onCancel;

        RefreshButtonListeners();
    }

    // View가 숨겨질 때 버튼 리스너와 콜백 참조를 정리합니다.
    // 같은 팝업 View를 재사용할 때 이전 콜백이 남아 중복 호출되는 것을 막습니다.
    protected override void OnHide()
    {
        ClearButtonListeners();
        confirmCallback = null;
        cancelCallback = null;
    }

    // Text 참조가 비어 있어도 팝업 전체가 멈추지 않도록 null을 허용합니다.
    private void SetText(Text targetText, string value)
    {
        if (targetText == null)
        {
            return;
        }

        targetText.text = value;
    }

    // 버튼 리스너를 최신 콜백 기준으로 다시 연결합니다.
    private void RefreshButtonListeners()
    {
        ClearButtonListeners();

        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(InvokeConfirm);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(InvokeCancel);
        }
    }

    // 버튼에 연결된 모든 리스너를 제거합니다.
    // 현재는 이 View가 버튼을 독점 제어한다는 전제로 사용합니다.
    private void ClearButtonListeners()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
        }
    }

    // 확인 버튼 클릭 이벤트를 외부 콜백으로 전달합니다.
    private void InvokeConfirm()
    {
        confirmCallback?.Invoke();
    }

    // 취소 버튼 클릭 이벤트를 외부 콜백으로 전달합니다.
    private void InvokeCancel()
    {
        cancelCallback?.Invoke();
    }
}
