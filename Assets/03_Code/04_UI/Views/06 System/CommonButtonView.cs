using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.EventSystems;

// 여러 UI 화면에서 공통으로 사용할 버튼 View
// 버튼 문구 표시 및 클릭 시 기능 동작만 실행
public class CommonButtonView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
    [Header("Core")]
    [SerializeField] private Button button;
    [SerializeField] private Text labelText;  
    // 버튼 전체의 Alpha값과 입력 차단을 위한 선택적 필드
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Visual")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private GameObject selectedObject;

    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = new Color(1f, 0.85f, 0.85f, 1f);
    [SerializeField] private Color selectedColor = new Color(1f, 0.55f, 0.55f, 1f);
    [SerializeField] private Color disabledColor = new Color(1f, 1f, 1f, 0.45f);

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioClip clickSound;

    // 버튼 클릭 시 실행할 Action
    private Action clickCallback;

    private bool isInteractable = true;
    private bool isSelected;
    private bool isPointerInside;

    private float InteractableAlpha = 1f;
    private float NonInteractiveAlpha = 0.45f;

    // 2026.08.10_UI 정리: 파괴 시 등록한 이벤트와 임시 UI 상태를 정리한다.
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
        RefreshVisual();
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
        this.isInteractable = isInteractable;

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

    // 2026.08.10_UI 정리: 선택 표시 값을 반영한다.
    public void SetSelected(bool isSelected)
    {
        this.isSelected = isSelected;
        RefreshVisual();
    }

    // 2026.08.10_UI 정리: UI 상태 상태를 정리한다.
    public void Clear()
    {
        ClearButtonListener();

        clickCallback = null;
        isPointerInside = false;
        isSelected = false;

        RefreshVisual();
    }

    // 2026.08.10_UI 정리: 포인터 상호작용에 맞춰 UI 표시 상태를 갱신한다.
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isInteractable)
            return;

        isPointerInside = true;
        PlaySound(hoverSound);
        RefreshVisual();
    }

    // 2026.08.10_UI 정리: 포인터 상호작용에 맞춰 UI 표시 상태를 갱신한다.
    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerInside = false;
        RefreshVisual();
    }

    // 2026.08.10_UI 정리: 포인터 상호작용에 맞춰 UI 표시 상태를 갱신한다.
    public void OnPointerDown(PointerEventData eventData)
    {
        if (!isInteractable)
            return;

        PlaySound(clickSound);
    }

    // 2026.08.10_UI 정리: 현재 데이터로 버튼 Listener 표시를 갱신한다.
    private void RefreshButtonListener()
    {
        if (button == null)
            return;

        button.onClick.AddListener(InvokeClick);
    }

    // 2026.08.10_UI 정리: 버튼 Listener 상태를 정리한다.
    private void ClearButtonListener()
    {
        if (button == null)
            return;

        button.onClick.RemoveListener(InvokeClick);
    }

    // 2026.08.10_UI 정리: 이 메서드의 UI 처리 역할을 수행한다.
    private void InvokeClick()
    {
        clickCallback?.Invoke();
    }

    // 2026.08.10_UI 정리: 현재 데이터로 비주얼 표시를 갱신한다.
    private void RefreshVisual()
    {
        if (selectedObject != null)
        {
            selectedObject.SetActive(isSelected);
        }

        if (backgroundImage == null)
            return;

        if (!isInteractable)
        {
            backgroundImage.color = disabledColor;
            return;
        }

        if (isSelected)
        {
            backgroundImage.color = selectedColor;
            return;
        }

        backgroundImage.color = isPointerInside ? hoverColor : normalColor;
    }

    // 2026.08.10_UI 정리: Sound UI 연출을 재생한다.
    private void PlaySound(AudioClip clip)
    {
        if (clip == null)
            return;

        if (audioSource != null)
        {
            audioSource.PlayOneShot(clip);
            return;
        }

        AudioSource.PlayClipAtPoint(clip, transform.position);
    }
}
