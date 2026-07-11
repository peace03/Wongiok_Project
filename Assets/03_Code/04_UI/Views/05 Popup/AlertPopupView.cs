using System;
using UnityEngine;
using UnityEngine.UI;

// 단순히 메시지와 확인 버튼만 있는 팝업 View입니다.
public class AlertPopupView : UIViewBase
{
    [SerializeField] private Text titleText;
    [SerializeField] private Text messageText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Text confirmButtonLabelText;

    // 확인 버튼 클릭 시 실행할 콜백
    private Action confirmCallback;

    public void Setup(
        string title,
        string message,
        string confirmText,
        Action onConfirm)
    {
        SetText(titleText, title);
        SetText(messageText, message);
        SetText(confirmButtonLabelText, confirmText);

        confirmCallback = onConfirm;

        RefreshButtonListeners();
    }

    protected override void OnHide()
    {
        ClearButtonListeners();
        confirmCallback = null;
    }

    // Text 참조가 비어 있어도 팝업 전체가 멈추지 않도록 null 값 포함
    private void SetText(Text targetText, string value)
    {
        if (targetText == null)
        {
            return;
        }

        targetText.text = value;
    }

    // 버튼 리스터를 최신 콜백 기준으로 다시 연결
    private void RefreshButtonListeners()
    {
        ClearButtonListeners();

        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(InvokeConfirm);
        }
    }

    // 버튼에 연결된 모든 리스터 제거
    private void ClearButtonListeners()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
        }
    }

    // 확인 버튼 클릭 이벤트를 외부 콜백으로 전달
    private void InvokeConfirm()
    {
        confirmCallback?.Invoke();
    }
}
