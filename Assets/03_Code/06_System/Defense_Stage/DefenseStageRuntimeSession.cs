using System.Collections.Generic;
using UnityEngine;

public static class DefenseStageRuntimeSession
{
    private static readonly HashSet<string> currentClearedStageIds =
        new HashSet<string>();

    private static readonly HashSet<string> checkpointClearedStageIds =
        new HashSet<string>();

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    // 애플리케이션 시작 시 정적 런타임 상태를 초기화합니다.
    private static void ResetOnApplicationStart()
    {
        ClearAll();
    }

    // 현재 진행에서 디펜스 스테이지가 클리어되었음을 기록합니다.
    public static void MarkStageCleared(string stageId)
    {
        if (string.IsNullOrWhiteSpace(stageId))
        {
            return;
        }

        currentClearedStageIds.Add(stageId);
    }

    // 현재 진행에서 해당 디펜스 스테이지가 클리어 상태인지 확인합니다.
    public static bool IsStageCleared(string stageId)
    {
        if (string.IsNullOrWhiteSpace(stageId))
        {
            return false;
        }

        return currentClearedStageIds.Contains(stageId);
    }

    // 현재 클리어 목록을 체크포인트 상태로 저장합니다.
    public static void CaptureCheckpointSnapshot()
    {
        checkpointClearedStageIds.Clear();

        foreach (string stageId in currentClearedStageIds)
        {
            checkpointClearedStageIds.Add(stageId);
        }
    }

    // 현재 클리어 목록을 마지막 체크포인트 상태로 되돌립니다.
    public static void RestoreCheckpointSnapshot()
    {
        currentClearedStageIds.Clear();

        foreach (string stageId in checkpointClearedStageIds)
        {
            currentClearedStageIds.Add(stageId);
        }
    }

    // 새 게임 시작 시 모든 디펜스 스테이지 상태를 초기화합니다.
    public static void ClearAll()
    {
        currentClearedStageIds.Clear();
        checkpointClearedStageIds.Clear();
    }
}