using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class PauseSkillInfoView : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Text nameText;
    [SerializeField] private Text levelText;
    [SerializeField] private Text descriptionText;
    [SerializeField] private GameObject equippedMarkObject;

    [SerializeField] private Sprite fallbackIcon;

    [SerializeField] private Image lockImage;
    [SerializeField] private Color unlockedIconColor = Color.white;
    [SerializeField] private Color lockedIconColor = new Color(0.35f, 0.35f, 0.35f, 1f);

    // 스킬 요약 설정
    public void Setup(UIPauseSkillInfoData data)
    {
        bool isLocked = !data.IsUnlocked || data.SkillId < 0;

        SetIcon(data.Icon);

        if (iconImage != null)
        {
            iconImage.color = isLocked
                ? lockedIconColor
                : unlockedIconColor;
        }

        SetText(nameText, isLocked ? string.Empty : data.SkillName);
        SetText(levelText, isLocked ? string.Empty : $"Lv.{data.Level}");
        SetText(descriptionText, data.Description);

        if (lockImage != null)
        {
            lockImage.gameObject.SetActive(isLocked);
        }

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

        if (iconImage != null)
        {
            iconImage.color = unlockedIconColor;
        }

        if (lockImage != null)
        {
            lockImage.gameObject.SetActive(false);
        }
    }

    // 2026.08.10_UI 정리: 상세 Visible 표시 값을 반영한다.
    public void SetDetailVisible(bool visible)
    {
        SetTextVisible(nameText, visible);
        SetTextVisible(descriptionText, visible);
    }

    // 2026.08.10_UI 정리: 텍스트 Visible 표시 값을 반영한다.
    private void SetTextVisible(Text targetText, bool visible)
    {
        if (targetText == null) return;

        targetText.gameObject.SetActive(visible);
    }

    // 스킬 아이콘 설정
    private void SetIcon(Sprite icon)
    {
        if (iconImage == null)
            return;

        // 스킬 아이콘이 없을 때 테스트용 fallback 아이콘 사용
        Sprite iconToShow = icon != null ? icon : fallbackIcon;

        // 실제 표시에는 원본 아이콘 또는 fallback 중 최종 선택된 Sprite를 넣음
        iconImage.sprite = iconToShow;
        iconImage.enabled = iconToShow != null;
    }

    // 스킬 요약 텍스트 설정
    private void SetText(Text targetText, string value)
    {
        if (targetText == null)
            return;

        targetText.text = value;
    }
}
