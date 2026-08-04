using UnityEngine;

[CreateAssetMenu(fileName = "New PassiveSkillData", menuName = "Data/Skill/Passive", order = 2)]
// 패시브 스킬 정보
public class PassiveSkillData : LevelBasedSkillData<PassiveSkillLevelData>
{
    // 객체 생성 함수
    public override SkillInstance CreateInstance(GameObject owner, ActiveSkillExecuter executer,
                                                                        CHAPTER_TYPE chapter, int fps)
                                                                => new(owner, executer, this, chapter, fps);
}