using UnityEngine;
using UnityEngine.SceneManagement;

// 2026.08.07_psb수정
public class CheckpointSaveSystem : MonoBehaviour
{
    public bool HasCheckpointSave =>
        UserSaveFileStore.TryLoad(out UserSaveFileData data) &&
        data.HasCheckpoint;

    // 기존 PlayerCheckpointTracker 호출부를 위해 현재 활성 체크포인트를 저장한다.
    public void SaveCheckpoint(PlayerCheckpointTracker targetTracker)
    {
        if (targetTracker == null || !targetTracker.HasActiveCheckpoint)
            return;

        CheckpointRuntimeData data = new CheckpointRuntimeData(
            null,
            string.Empty,
            targetTracker.ActiveCheckpointNumber,
            SceneManager.GetActiveScene().path,
            targetTracker.RespawnPosition,
            Vector3.zero,
            targetTracker.SavedHP,
            targetTracker.SavedHealItemCount);

        SaveCheckpoint(data);
    }

    // 체크포인트의 위치와 당시 진행도를 하나의 JSON 저장 파일에 기록한다.
    public void SaveCheckpoint(CheckpointRuntimeData data)
    {
        if (!data.IsValid)
            return;

        UserSaveFileData saveData = UserSaveFileStore.LoadOrCreate();
        saveData.HasSaveData = true;
        saveData.HasCheckpoint = true;
        saveData.CheckpointId = data.CheckpointId ?? string.Empty;
        saveData.CheckpointNumber = data.DisplayNumber;
        saveData.CheckpointScenePath = data.ScenePath ?? string.Empty;
        saveData.RespawnPosition = data.RespawnPosition;
        saveData.RespawnEulerAngles = data.RespawnEulerAngles;
        saveData.SavedHP = data.SavedHP;
        saveData.SavedHealItemCount = data.SavedHealItemCount;
        saveData.ProgressJson = data.Progress.IsValid
            ? JsonUtility.ToJson(data.Progress)
            : string.Empty;

        UserSaveFileStore.Save(saveData);
    }

    // 현재 런타임 세션에 등록된 체크포인트를 저장한다.
    public void SaveCheckpoint()
    {
        if (!CheckpointRuntimeSession.HasActiveCheckpoint)
            return;

        SaveCheckpoint(CheckpointRuntimeSession.Current);
    }

    // 저장된 ID를 Catalog로 보정한 뒤 일반 스테이지 부활에 사용할 데이터를 복원한다.
    public bool TryLoadCheckpoint(
        CheckpointCatalog catalog,
        out CheckpointRuntimeData data)
    {
        data = default;

        if (!UserSaveFileStore.TryLoad(out UserSaveFileData saveData) ||
            !saveData.HasCheckpoint)
        {
            return false;
        }

        CheckpointDefinition definition = null;
        if (catalog != null)
            catalog.TryFind(saveData.CheckpointId, out definition);

        int displayNumber = saveData.CheckpointNumber;
        string scenePath = saveData.CheckpointScenePath;
        Vector3 respawnPosition = saveData.RespawnPosition;
        Vector3 respawnEulerAngles = saveData.RespawnEulerAngles;

        if (definition != null && definition.IsValid)
        {
            displayNumber = definition.DisplayNumber;
            scenePath = definition.ScenePath;
            respawnPosition = definition.RespawnPosition;
            respawnEulerAngles = definition.RespawnEulerAngles;
        }

        CheckpointProgressSnapshot progress = default;
        if (!string.IsNullOrWhiteSpace(saveData.ProgressJson))
        {
            progress = JsonUtility.FromJson<CheckpointProgressSnapshot>(
                saveData.ProgressJson);
        }

        data = new CheckpointRuntimeData(
            definition,
            saveData.CheckpointId,
            displayNumber,
            scenePath,
            respawnPosition,
            respawnEulerAngles,
            saveData.SavedHP,
            saveData.SavedHealItemCount,
            progress);

        return data.IsValid;
    }

    // 저장 파일의 체크포인트를 현재 런타임 세션에도 등록한다.
    public bool RestoreRuntimeSession(CheckpointCatalog catalog)
    {
        if (!TryLoadCheckpoint(catalog, out CheckpointRuntimeData data))
            return false;

        CheckpointRuntimeSession.SetActiveCheckpoint(data);
        return true;
    }

    // 기존 호출부가 필요한 단일 값 조회를 JSON 데이터에서 제공한다.
    public int LoadCheckpointNumber()
    {
        return UserSaveFileStore.TryLoad(out UserSaveFileData data)
            ? data.CheckpointNumber
            : -1;
    }

    public string LoadScenePath()
    {
        return UserSaveFileStore.TryLoad(out UserSaveFileData data)
            ? data.CheckpointScenePath ?? string.Empty
            : string.Empty;
    }

    public Vector3 LoadRespawnPosition()
    {
        return UserSaveFileStore.TryLoad(out UserSaveFileData data)
            ? data.RespawnPosition
            : Vector3.zero;
    }

    public float LoadSavedHP()
    {
        return UserSaveFileStore.TryLoad(out UserSaveFileData data)
            ? data.SavedHP
            : 0f;
    }

    public int LoadSavedHealItemCount()
    {
        return UserSaveFileStore.TryLoad(out UserSaveFileData data)
            ? data.SavedHealItemCount
            : 0;
    }

    // 저장 파일은 남기고 체크포인트 데이터만 비운다.
    public void DeleteCheckpointSave()
    {
        UserSaveFileStore.ClearCheckpoint();
    }
}
