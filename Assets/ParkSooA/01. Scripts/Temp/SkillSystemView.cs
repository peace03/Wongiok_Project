using UnityEngine;

//public struct UISetPlayerSkillSlotsEvent
//{
//    public UIPlayerSkillSlotData[] SkillSlots { get; private set; }

//    public UISetPlayerSkillSlotsEvent(UIPlayerSkillSlotData[] skillSlots) => SkillSlots = skillSlots;
//}

//// UI용 플레이어 스킬 슬롯 데이터
//public struct UIPlayerSkillSlotData
//{
//    public Sprite Icon { get; private set; }                // 아이콘
//    public string KeyText { get; private set; }             // 스킬 사용 키
//    public int Level { get; private set; }                  // 레벨
//    public float CooldownProgress { get; private set; }     // 쿨타임
//    public bool IsAvailable { get; private set; }           // 사용 가능 여부

//    // 생성자
//    public UIPlayerSkillSlotData(Sprite icon, string keyText, int level, float cooldown, bool available)
//    {
//        Icon = icon;
//        KeyText = keyText;
//        Level = level;
//        CooldownProgress = cooldown;
//        IsAvailable = available;
//    }
//}

public class SkillSystemView : MonoBehaviour
{
    // 스킬 슬롯들 갱신 함수
    private void RefreshSkillSlots(UIPlayerSkillSlotData[] skillSlots)
    {
        Debug.Log("스킬 슬롯들 갱신 함수");
    }

    // 스킬 슬롯 갱신 함수
    private void RefreshSkillSlot(int index, bool hasData, UIPlayerSkillSlotData skillSlot)
    {
        Debug.Log("스킬 슬롯 갱신 함수");
    }
}