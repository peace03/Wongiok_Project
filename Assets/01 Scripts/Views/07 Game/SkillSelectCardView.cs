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

    private void OnDestroy()
    {
        Clear();
    }

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

    public void SetInteractable(bool isInteractable)
    {
        if (button != null)
        {
            button.interactable = isInteractable;
        }
    }

    private void RefreshButtonListener()
    {
        if (button == null)
            return;

        button.onClick.AddListener(HandleClicked);
    }

    private void ClearButtonListener()
    {
        if (button == null)
            return;

        button.onClick.RemoveListener(HandleClicked);
    }

    private void HandleClicked()
    {
        selectedCallback?.Invoke(optionIndex, skillId);
    }

    private void SetIcon(Sprite icon)
    {
        if (iconImage == null)
            return;

        iconImage.sprite = icon;
        iconImage.enabled = icon != null;
    }

    private void SetText(Text targetText, string value)
    {
        if (targetText == null)
            return;

        targetText.text = value;
    }
}
