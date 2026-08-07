using System;
using System.IO;
using System.Text;
using UnityEngine;

// 2026.08.07_psb수정
[Serializable]
public sealed class UserSaveFileData
{
    public bool HasSaveData;
    public bool HasCheckpoint;
    public string CheckpointId;
    public int CheckpointNumber = -1;
    public string CheckpointScenePath;
    public Vector3 RespawnPosition;
    public Vector3 RespawnEulerAngles;
    public float SavedHP;
    public int SavedHealItemCount;
    public string ProgressJson;
}

// 2026.08.07_psb수정
public static class UserSaveFileStore
{
    private const string SaveDirectoryName = "save";
    private const string SaveFileName = "user-save.json";

    internal static string SaveDirectoryOverrideForTests { get; set; }

    public static string SaveFilePath => Path.Combine(
        GetSaveDirectoryPath(),
        SaveFileName);

    public static bool HasSaveData =>
        TryLoad(out UserSaveFileData data) && data.HasSaveData;

    // 빌드에서는 실행 파일 옆 save 폴더, 에디터에서는 persistentDataPath를 사용한다.
    private static string GetSaveDirectoryPath()
    {
        if (!string.IsNullOrWhiteSpace(SaveDirectoryOverrideForTests))
            return SaveDirectoryOverrideForTests;

#if UNITY_EDITOR
        return Path.Combine(Application.persistentDataPath, SaveDirectoryName);
#else
        DirectoryInfo buildDirectory = Directory.GetParent(Application.dataPath);
        string rootDirectory = buildDirectory == null
            ? Application.dataPath
            : buildDirectory.FullName;

        return Path.Combine(rootDirectory, SaveDirectoryName);
#endif
    }

    // 저장 파일이 유효할 때만 데이터를 반환해 손상된 파일을 이어하기 데이터로 사용하지 않는다.
    public static bool TryLoad(out UserSaveFileData data)
    {
        data = null;
        string saveFilePath = SaveFilePath;

        if (!File.Exists(saveFilePath))
            return false;

        try
        {
            string json = File.ReadAllText(saveFilePath);
            if (string.IsNullOrWhiteSpace(json))
                return false;

            data = JsonUtility.FromJson<UserSaveFileData>(json);
            return data != null;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"저장 파일을 읽지 못했습니다: {exception.Message}");
            data = null;
            return false;
        }
    }

    // 기존 파일을 보존해 갱신하거나, 없는 경우 빈 저장 데이터를 만든다.
    public static UserSaveFileData LoadOrCreate()
    {
        return TryLoad(out UserSaveFileData data)
            ? data
            : new UserSaveFileData();
    }

    // 임시 파일을 완성한 뒤 교체해 저장 중 종료되어도 기존 JSON을 보존한다.
    public static void Save(UserSaveFileData data)
    {
        if (data == null)
            return;

        string directoryPath = GetSaveDirectoryPath();
        string saveFilePath = Path.Combine(directoryPath, SaveFileName);
        string temporaryFilePath = saveFilePath + ".tmp";

        try
        {
            Directory.CreateDirectory(directoryPath);
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(temporaryFilePath, json, new UTF8Encoding(false));

            if (File.Exists(saveFilePath))
            {
                File.Replace(temporaryFilePath, saveFilePath, null);
            }
            else
            {
                File.Move(temporaryFilePath, saveFilePath);
            }
        }
        catch (Exception exception)
        {
            Debug.LogError($"저장 파일을 기록하지 못했습니다: {exception.Message}");
        }
        finally
        {
            if (File.Exists(temporaryFilePath))
                File.Delete(temporaryFilePath);
        }
    }

    // 새 게임 시작 시 이어하기 가능 여부만 만들고 체크포인트는 비운다.
    public static void MarkSaveDataExists()
    {
        UserSaveFileData data = LoadOrCreate();
        data.HasSaveData = true;
        Save(data);
    }

    // 처음부터 재시작 등에서 체크포인트만 삭제하고 이어하기 파일은 유지한다.
    public static void ClearCheckpoint()
    {
        if (!TryLoad(out UserSaveFileData data))
            return;

        data.HasCheckpoint = false;
        data.CheckpointId = string.Empty;
        data.CheckpointNumber = -1;
        data.CheckpointScenePath = string.Empty;
        data.RespawnPosition = Vector3.zero;
        data.RespawnEulerAngles = Vector3.zero;
        data.SavedHP = 0f;
        data.SavedHealItemCount = 0;
        data.ProgressJson = string.Empty;
        Save(data);
    }
}
