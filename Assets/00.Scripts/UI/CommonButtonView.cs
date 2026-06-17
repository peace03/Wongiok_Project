using UnityEngine;
using UnityEngine.UI;
using System;

// 여러 UI 화면에서 공통으로 사용할 버튼 View
// 버튼 문구 표시 및 클릭 시 기능 동작만 실행
public class CommonButtonView : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Text labelText;
    
    // 버튼 전체의 Alpha값과 입력 차단을 위한 선택적 필드
    [SerializeField] private CanvasGroup canvasGroup;

    // 버튼 클릭 시 실행할 Action
    private Action clickCallback;

    private float InteractableAlpha = 1f;
    private float NonInteractiveAlpha = 0.45f;

    private void OnDestroy()
    {
        Clear();
    }

    // 버튼 텍스트, 클릭 동작, 상호작용 가능 여부 설정
    // 같은 버튼 재사용을 위해 기존 리스너 초기화 후 새 콜백 연결
    public void Setup(string label, Action onClick, bool isInteractable = true)
    {
        ClearButtonListener();

        clickCallback = onClick;

        SetLabel(label);
        SetInteractable(isInteractable);
        RefreshButtonListener();

    }

    // 버튼 텍스트 설정
    public void SetLabel(string label)
    {
        if (labelText == null)
            return;

        labelText.text = label;
    }

    // 버튼 상호작용 가능 여부 설정
    // 버튼 상호작용 가능 여부를 제어
    // CanvasGroup은 선택적으로 시각적 비활성화와 Raycast 차단을 함께 처리
    public void SetInteractable(bool isInteractable)
    {
        if (button != null)
        {
            button.interactable = isInteractable;
        }

        if (canvasGroup == null)
            return;

        canvasGroup.alpha = isInteractable ? InteractableAlpha : NonInteractiveAlpha;
        canvasGroup.interactable = isInteractable;
        canvasGroup.blocksRaycasts = isInteractable;
    }

    public void Clear()
    {
        ClearButtonListener();
        clickCallback = null;
    }

    private void RefreshButtonListener()
    {
        if (button == null)
            return;

        button.onClick.AddListener(InvokeClick);
    }

    private void ClearButtonListener()
    {
        if (button == null)
            return;

        button.onClick.RemoveListener(InvokeClick);
    }

    private void InvokeClick()
    {
        clickCallback?.Invoke();
    }
}
