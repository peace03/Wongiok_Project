using System;
using UnityEngine;

[Serializable]
public struct CheckpointSkillSnapshot
{
    public int SkillId;
    public int Level;
    public int EquippedActiveSlot;
}

[Serializable]
public struct CheckpointProgressSnapshot
{
    public bool IsValid;
    public float CurrentHP;
    public int HealItemCount;
    public int LifeCount;
    public int PlayerLevel;
    public float CurrentExperience;
    public PlayerPersistentStatSnapshot PersistentStats;
    public CheckpointSkillSnapshot[] Skills;
    public string[] ClearedDefenseStageIds;
}

[Serializable]
public readonly struct CheckpointRuntimeData
{
    public CheckpointDefinition Definition { get; }
    public string CheckpointId { get; }
    public int DisplayNumber { get; }
    public string ScenePath { get; }
    public Vector3 RespawnPosition { get; }
    public Vector3 RespawnEulerAngles { get; }
    public float SavedHP { get; }
    public int SavedHealItemCount { get; }
    public CheckpointProgressSnapshot Progress { get; }

    public Quaternion RespawnRotation =>
        Quaternion.Euler(RespawnEulerAngles);

    public bool IsValid =>
        !string.IsNullOrWhiteSpace(ScenePath);

    // 런타임에서 사용할 체크포인트 스냅샷을 생성합니다
    public CheckpointRuntimeData(
        CheckpointDefinition definition,
        string checkpointId,
        int displayNumber,
        string scenePath,
        Vector3 respawnPosition,
        Vector3 respawnEulerAngles,
        float savedHP,
        int savedHealItemCount,
        CheckpointProgressSnapshot progress = default)
    {
        Definition = definition;
        CheckpointId = checkpointId;
        DisplayNumber = displayNumber;
        ScenePath = scenePath;
        RespawnPosition = respawnPosition;
        RespawnEulerAngles = respawnEulerAngles;
        SavedHP = savedHP;
        SavedHealItemCount = savedHealItemCount;
        Progress = progress;
    }

    // CheckpointDefinition과 플레이어 스냅샷으로 런타임 데이터를 만듭니다
    public static CheckpointRuntimeData FromDefinition(
        CheckpointDefinition definition,
        float savedHP,
        int savedHealItemCount,
        CheckpointProgressSnapshot progress = default)
    {
        if (definition == null)
        {
            return default;
        }

        return new CheckpointRuntimeData(
            definition,
            definition.CheckpointId,
            definition.DisplayNumber,
            definition.ScenePath,
            definition.RespawnPosition,
            definition.RespawnEulerAngles,
            savedHP,
            savedHealItemCount,
            progress);
    }
}

public static class CheckpointRuntimeSession
{
    private static CheckpointRuntimeData current;
    private static bool hasActiveCheckpoint;
    private static int pendingLifeCount = -1;

    public static bool HasActiveCheckpoint =>
        hasActiveCheckpoint;

    public static CheckpointRuntimeData Current => current;

    public static int PendingLifeCount => pendingLifeCount;

    // 가장 최근에 활성화된 체크포인트 데이터를 저장합니다
    public static void SetActiveCheckpoint(
        CheckpointRuntimeData data)
    {
        current = data;
        hasActiveCheckpoint = data.IsValid;
    }

    // 씬 이동 후 복원할 남은 잔기 수를 저장합니다
    public static void SetPendingLifeCount(int lifeCount)
    {
        pendingLifeCount = lifeCount;
    }

    // 씬 이동 후 사용한 임시 잔기 정보를 제거합니다
    public static void ClearPendingLifeCount()
    {
        pendingLifeCount = -1;
    }

    // 새 게임을 시작할 때 모든 런타임 체크포인트 정보를 제거합니다
    public static void ClearAll()
    {
        current = default;
        hasActiveCheckpoint = false;
        pendingLifeCount = -1;
    }
}
