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
    public PlayerPersistentStatSnapshot PersistentStats;
    public PrototypeSkillState[] Skills;
    public int[] OwnedSkillOrder;

    public PrototypeProgressSnapshot Clone()
    {
        return new PrototypeProgressSnapshot
        {
            Level = Level,
            CurrentExp = CurrentExp,
            RequiredExp = RequiredExp,
            MaxHp = MaxHp,
            PersistentStats = PersistentStats,
            Skills = Skills == null ? Array.Empty<PrototypeSkillState>() : Skills.ToArray(),
            OwnedSkillOrder = OwnedSkillOrder == null ? Array.Empty<int>() : OwnedSkillOrder.ToArray()
        };
    }
}

public static class PrototypeGameSession
{
    // 2026.08.07_psb수정

    public static int HighestClearedChapterId { get; private set;  }
    public static int CurrentChapterId { get; private set; } = 1;
    public static bool HasSaveData =>
        UserSaveFileStore.HasSaveData;
    public static bool HasCheckpoint => checkpointSnapshot != null;

    private static PrototypeProgressSnapshot committedSnapshot;
    private static PrototypeProgressSnapshot chapterStartSnapshot;
    private static PrototypeProgressSnapshot checkpointSnapshot;
    private static int pendingTitleCardChapterId = -1;
    private static bool skipPrologueOnNextLobbyEnter;

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
        HighestClearedChapterId = 0;
        CurrentChapterId = 1;
        pendingTitleCardChapterId = -1;
        skipPrologueOnNextLobbyEnter = false;
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
            },
            OwnedSkillOrder = new[]
            {
                1004, 1005, 1006, 1007,
                1008, 1009, 1010, 1011, 1012,
                1013, 1014, 1015, 1016, 1017,
                1018, 1019, 1020, 1021
            }
        };

        chapterStartSnapshot = committedSnapshot.Clone();
    }

    public static void StartNewGame()
    {
        ResetAll();
        MarkSaveDataExists();
        UserSaveFileStore.ClearCheckpoint();
    }

    // 2026.08.07_psb수정
    // 게임오버에서 처음부터 다시 시작할 때 런 진행도만 기본값으로 되돌린다.
    public static void RestartRunFromBeginning()
    {
        ResetAll();
        MarkSaveDataExists();
        UserSaveFileStore.ClearCheckpoint();
    }

    public static void BeginChapter(int chapterId)
    {
        EnsureInitialized();

        MarkSaveDataExists();
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

    public static void ClearCheckpoint()
    {
        checkpointSnapshot = null;
    }

    public static void CommitChapterClear(
        int chapterId, PrototypeProgressSnapshot currentProgress)
    {
        EnsureInitialized();

        if (currentProgress == null) return;

        MarkSaveDataExists();

        HighestClearedChapterId = Mathf.Max(HighestClearedChapterId, chapterId);

        PrototypeProgressSnapshot result = currentProgress.Clone();

        List<PrototypeSkillState> skills = 
            result.Skills == null 
            ? new List<PrototypeSkillState>() 
            : result.Skills.ToList();

        if (chapterId == 1)
        {
            for (int skillId = 1004; skillId <= 1021; skillId++)
            {
                AddUnlockedSkill(skills, skillId);
            }
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

    public static bool TryConsumeSkipPrologueOnNextLobbyEnter()
    {
        bool shouldSkipPrologue = skipPrologueOnNextLobbyEnter;
        skipPrologueOnNextLobbyEnter = false;

        return shouldSkipPrologue;
    }

    public static void ReturnToMainMenu()
    {
        checkpointSnapshot = null;
        chapterStartSnapshot = committedSnapshot.Clone();
        pendingTitleCardChapterId = -1;
        skipPrologueOnNextLobbyEnter = true;
    }

    private static void AddUnlockedSkill(
        List<PrototypeSkillState> skills,
        int skillId)
    {
        if (skills.Any(skill => skill.SkillId == skillId)) return;

        skills.Add(new PrototypeSkillState(skillId, 1, -1));
    }

    // 2026.08.07_psb수정
    // 새 게임을 시작한 기록을 앱 재실행 뒤에도 타이틀에서 확인할 수 있도록 저장한다.
    private static void MarkSaveDataExists()
    {
        UserSaveFileStore.MarkSaveDataExists();
    }
}
