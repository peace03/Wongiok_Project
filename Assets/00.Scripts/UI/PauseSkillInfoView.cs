using UnityEngine;
using UnityEngine.UI;

public class PauseSkillInfoView : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Text nameText;
    [SerializeField] private Text levelText;
    [SerializeField] private Text descriptionText;
    [SerializeField] private GameObject equippedMarkObject;

    // 스킬 요약 설정
    public void Setup(UIPauseSkillInfoData data)
    {
        SetIcon(data.Icon);
        SetText(nameText, data.SkillName);
        SetText(levelText, $"Lv.{data.Level}");
        SetText(descriptionText, data.Description);

        if (equippedMarkObject != null)
        {
            equippedMarkObject.SetActive(data.IsEquipped);
        }
    }

    // 초기화
    public void Clear()
    {
        SetIcon(null);
        SetText(nameText, string.Empty);
        SetText(levelText, string.Empty);
        SetText(descriptionText, string.Empty);

        if (equippedMarkObject != null)
        {
            equippedMarkObject.SetActive(false);
        }
    }

    // 스킬 아이콘 설정
    private void SetIcon(Sprite icon)
    {
        if (iconImage == null)
            return;

        iconImage.sprite = icon;
        iconImage.enabled = icon != null;
    }

    // 스킬 요약 텍스트 설정
    private void SetText(Text targetText, string value)
    {
        if (targetText == null)
            return;

        targetText.text = value;
    }
}
