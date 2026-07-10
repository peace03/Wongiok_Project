using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public struct PrototypeSkillState
{
    public int SkillId;
    public int Level;
    public int SlotIndex;

    public PrototypeSkillState(int skillId, int level, int slotIndex)
    {
        SkillId = skillId;
        Level = level;
        SlotIndex = slotIndex;
    }
}

[Serializable]
public sealed class PrototypeProgressSnapshot
{
    public int Level;
    public float CurrentExp;
    public float RequiredExp;
    public float MaxHp;
    public PrototypeSkillState[] Skills;

    public PrototypeProgressSnapshot Clone()
    {
        return new PrototypeProgressSnapshot
        {
            Level = Level,
            CurrentExp = CurrentExp,
            RequiredExp = RequiredExp,
            MaxHp = MaxHp,
            Skills = Skills == null ? Array.Empty<PrototypeSkillState>() : Skills.ToArray()
        };
    }
}

public static class PrototypeGameSession
{
    public static int HighestClearedChapterId { get; private set;  }
    public static int CurrentChapterId { get; private set; } = 1;
    public static bool HasSaveData { get; private set;  }
    public static bool HasCheckpoint => checkpointSnapshot != null;

    private static PrototypeProgressSnapshot committedSnapshot;
    private static PrototypeProgressSnapshot chapterStartSnapshot;
    private static PrototypeProgressSnapshot checkpointSnapshot;
    private static int pendingTitleCardChapterId = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatic()
    {
        ResetAll();
    }

    public static void EnsureInitialized()
    {
        if (committedSnapshot != null) return;

        ResetAll();
    }

    public static void ResetAll()
    {
        HasSaveData = false;

        HighestClearedChapterId = 0;
        CurrentChapterId = 1;
        pendingTitleCardChapterId = -1;
        checkpointSnapshot = null;

        committedSnapshot = new PrototypeProgressSnapshot
        {
            Level = 1,
            CurrentExp = 0f,
            RequiredExp = 100f,
            MaxHp = 100f,
            Skills = new[]
            {
                new PrototypeSkillState(1001, 1, 0),
                new PrototypeSkillState(1002, 1, 1),
                new PrototypeSkillState(1003, 1, 2)
            }
        };

        chapterStartSnapshot = committedSnapshot.Clone();
    }

    public static void StartNewGame()
    {
        ResetAll();
        HasSaveData = true;
    }

    public static void BeginChapter(int chapterId)
    {
        EnsureInitialized();

        HasSaveData = true;
        CurrentChapterId = chapterId;
        checkpointSnapshot = null;
        chapterStartSnapshot = committedSnapshot.Clone();
    }

    public static PrototypeProgressSnapshot GetChapterStart()
    {
        EnsureInitialized();
        return chapterStartSnapshot.Clone();
    }

    public static void SaveCheckpoint(PrototypeProgressSnapshot snapshot)
    {
        checkpointSnapshot = snapshot?.Clone();
    }

    public static bool TryGetCheckpoint(out PrototypeProgressSnapshot snapshot)
    {
        snapshot = checkpointSnapshot?.Clone();
        return snapshot != null;
    }

    public static void ClearCheckpoint()
    {
        checkpointSnapshot = null;
    }

    public static void CommitChapterClear(
        int chapterId, PrototypeProgressSnapshot currentProgress)
    {
        EnsureInitialized();

        if (currentProgress == null) return;

        HasSaveData = true;

        HighestClearedChapterId = Mathf.Max(HighestClearedChapterId, chapterId);

        PrototypeProgressSnapshot result = currentProgress.Clone();

        List<PrototypeSkillState> skills = 
            result.Skills == null 
            ? new List<PrototypeSkillState>() 
            : result.Skills.ToList();

        if (chapterId == 1)
        {
            AddUnlockedSkill(skills, 1004);
            AddUnlockedSkill(skills, 1005);
        }

        result.Skills = skills.ToArray();

        committedSnapshot = result;
        chapterStartSnapshot = result.Clone();
        checkpointSnapshot = null;
    }

    public static void PrepareNextChapter(int chapterId)
    {
        BeginChapter(chapterId);
        pendingTitleCardChapterId = chapterId;
    }

    public static bool TryConsumePendingTitleCard(out int chapterId)
    {
        chapterId = pendingTitleCardChapterId;
        pendingTitleCardChapterId = -1;
        return chapterId >= 0;
    }

    public static void ReturnToMainMenu()
    {
        checkpointSnapshot = null;
        chapterStartSnapshot = committedSnapshot.Clone();
        pendingTitleCardChapterId = -1;
    }

    private static void AddUnlockedSkill(
        List<PrototypeSkillState> skills,
        int skillId)
    {
        if (skills.Any(skill => skill.SkillId == skillId)) return;

        skills.Add(new PrototypeSkillState(skillId, 1, -1));
    }
}
