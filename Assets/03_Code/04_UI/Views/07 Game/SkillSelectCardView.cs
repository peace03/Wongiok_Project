using UnityEngine;
using UnityEngine.UI;
using System;


// 레벨업 화면에서 하나의 스킬 후보 카드를 표시하는 View
public class SkillSelectCardView : MonoBehaviour
{
    [Header("Button")]
    [SerializeField] private Button button;

    [Header("Skill Info")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Text nameText;
    [SerializeField] private Text levelText;
    [SerializeField] private Text descriptionText;
    [SerializeField] private Text effectText;
    [SerializeField] private GameObject maxLevelObject;

    private int optionIndex;
    private int skillId;
    private Action<int, int> selectedCallback;

    // 2026.08.10_UI 정리: 파괴 시 등록한 이벤트와 임시 UI 상태를 정리한다.
    private void OnDestroy()
    {
        Clear();
    }

    // 2026.08.10_UI 정리: 전달받은 데이터와 콜백으로 UI 상태 상태를 설정한다.
    public void Setup(UILevelUpSkillOptionData data, int optionIndex, Action<int, int> onSelected)
    {
        ClearButtonListener();

        this.optionIndex = optionIndex;
        skillId = data.SkillId;
        selectedCallback = onSelected;

        gameObject.SetActive(true);

        SetIcon(data.Icon);
        SetText(nameText, data.SkillName);
        SetText(levelText, data.IsMaxLevel ? "MAX" : $"Lv.{data.CurrentLevel} > Lv.{data.NextLevel}");
        SetText(descriptionText, data.Description);
        SetText(effectText, data.EffectText);

        if (maxLevelObject != null)
        {
            maxLevelObject.SetActive(data.IsMaxLevel);
        }

        SetInteractable(true);
        RefreshButtonListener();
    }

    // 2026.08.10_UI 정리: UI 상태 상태를 정리한다.
    public void Clear()
    {
        ClearButtonListener();

        selectedCallback = null;
        optionIndex = -1;
        skillId = 0;

        SetIcon(null);
        SetText(nameText, string.Empty);
        SetText(levelText, string.Empty);
        SetText(descriptionText, string.Empty);
        SetText(effectText, string.Empty);

        if (maxLevelObject != null)
        {
            maxLevelObject.SetActive(false);
        }

        gameObject.SetActive(false);
    }

    // 2026.08.10_UI 정리: Interactable 표시 값을 반영한다.
    public void SetInteractable(bool isInteractable)
    {
        if (button != null)
        {
            button.interactable = isInteractable;
        }
    }

    // 2026.08.10_UI 정리: 현재 데이터로 버튼 Listener 표시를 갱신한다.
    private void RefreshButtonListener()
    {
        if (button == null)
            return;

        button.onClick.AddListener(HandleClicked);
    }

    // 2026.08.10_UI 정리: 버튼 Listener 상태를 정리한다.
    private void ClearButtonListener()
    {
        if (button == null)
            return;

        button.onClick.RemoveListener(HandleClicked);
    }

    // 2026.08.10_UI 정리: 클릭 관련 입력 또는 EventBus 요청을 처리한다.
    private void HandleClicked()
    {
        selectedCallback?.Invoke(optionIndex, skillId);
    }

    // 2026.08.10_UI 정리: 아이콘 표시 값을 반영한다.
    private void SetIcon(Sprite icon)
    {
        if (iconImage == null)
            return;

        iconImage.sprite = icon;
        iconImage.enabled = icon != null;
    }

    // 2026.08.10_UI 정리: 텍스트 표시 값을 반영한다.
    private void SetText(Text targetText, string value)
    {
        if (targetText == null)
            return;

        targetText.text = value;
    }
}
