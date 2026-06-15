// 스킬 뷰 인터페이스
public interface ISkillView
{
    // 스킬 슬롯들 갱신 함수
    void RefreshSkillSlots(UIPlayerSkillSlotData[] skillSlots);
    // 스킬 슬롯 갱신 함수
    void RefreshSkillSlot(int index, bool hasData, UIPlayerSkillSlotData slotData);
}