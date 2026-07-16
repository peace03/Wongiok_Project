using UnityEngine;
using UnityEngine.SceneManagement;

public class CheckpointSaveSystem : MonoBehaviour
{
    private const string HasSaveKey = "Checkpoint.HasSave";
    private const string IdKey = "Checkpoint.Id";
    private const string NumberKey = "Checkpoint.Number";
    private const string ScenePathKey = "Checkpoint.ScenePath";
    private const string PositionXKey = "Checkpoint.PositionX";
    private const string PositionYKey = "Checkpoint.PositionY";
    private const string PositionZKey = "Checkpoint.PositionZ";
    private const string RotationXKey = "Checkpoint.RotationX";
    private const string RotationYKey = "Checkpoint.RotationY";
    private const string RotationZKey = "Checkpoint.RotationZ";
    private const string HpKey = "Checkpoint.HP";
    private const string HealItemCountKey =
        "Checkpoint.HealItemCount";

    public bool HasCheckpointSave =>
        PlayerPrefs.GetInt(HasSaveKey, 0) == 1;


    // 기존 PlayerCheckpointTracker 호출부를 위한 호환 저장을 수행합니다
    public void SaveCheckpoint(
        PlayerCheckpointTracker targetTracker)
    {
        if (targetTracker == null ||
            !targetTracker.HasActiveCheckpoint)
        {
            return;
        }

        CheckpointRuntimeData data =
            new CheckpointRuntimeData(
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

    // 전달된 ScriptableObject 기반 체크포인트 데이터를 저장합니다
    public void SaveCheckpoint(CheckpointRuntimeData data)
    {
        if (!data.IsValid)
        {
            return;
        }

        PlayerPrefs.SetInt(HasSaveKey, 1);
        PlayerPrefs.SetString(
            IdKey,
            data.CheckpointId ?? string.Empty);
        PlayerPrefs.SetInt(
            NumberKey,
            data.DisplayNumber);
        PlayerPrefs.SetString(
            ScenePathKey,
            data.ScenePath ?? string.Empty);

        SaveVector3(
            data.RespawnPosition,
            PositionXKey,
            PositionYKey,
            PositionZKey);
        SaveVector3(
            data.RespawnEulerAngles,
            RotationXKey,
            RotationYKey,
            RotationZKey);

        PlayerPrefs.SetFloat(HpKey, data.SavedHP);
        PlayerPrefs.SetInt(
            HealItemCountKey,
            data.SavedHealItemCount);
        PlayerPrefs.Save();
    }

    // 현재 런타임 체크포인트가 있으면 해당 데이터를 저장합니다
    public void SaveCheckpoint()
    {
        if (!CheckpointRuntimeSession.HasActiveCheckpoint)
        {
            return;
        }

        SaveCheckpoint(CheckpointRuntimeSession.Current);
    }

    // 저장 데이터와 Catalog를 사용해 런타임 체크포인트를 복원합니다
    public bool TryLoadCheckpoint(
        CheckpointCatalog catalog,
        out CheckpointRuntimeData data)
    {
        data = default;

        if (!HasCheckpointSave)
        {
            return false;
        }

        string checkpointId =
            PlayerPrefs.GetString(IdKey, string.Empty);

        CheckpointDefinition definition = null;

        if (catalog != null)
        {
            catalog.TryFind(
                checkpointId,
                out definition);
        }

        int displayNumber =
            PlayerPrefs.GetInt(NumberKey, -1);
        string scenePath =
            PlayerPrefs.GetString(
                ScenePathKey,
                string.Empty);
        Vector3 respawnPosition =
            LoadVector3(
                PositionXKey,
                PositionYKey,
                PositionZKey);
        Vector3 respawnEulerAngles =
            LoadVector3(
                RotationXKey,
                RotationYKey,
                RotationZKey);

        if (definition != null &&
            definition.IsValid)
        {
            displayNumber = definition.DisplayNumber;
            scenePath = definition.ScenePath;
            respawnPosition =
                definition.RespawnPosition;
            respawnEulerAngles =
                definition.RespawnEulerAngles;
        }

        data = new CheckpointRuntimeData(
            definition,
            checkpointId,
            displayNumber,
            scenePath,
            respawnPosition,
            respawnEulerAngles,
            PlayerPrefs.GetFloat(HpKey, 0f),
            PlayerPrefs.GetInt(
                HealItemCountKey,
                0));

        return data.IsValid;
    }

    // 저장된 체크포인트를 런타임 세션에 적용합니다
    public bool RestoreRuntimeSession(
        CheckpointCatalog catalog)
    {
        if (!TryLoadCheckpoint(
                catalog,
                out CheckpointRuntimeData data))
        {
            return false;
        }

        CheckpointRuntimeSession.SetActiveCheckpoint(data);
        return true;
    }

    // Vector3 값을 세 개의 PlayerPrefs 키로 저장합니다
    private void SaveVector3(
        Vector3 value,
        string xKey,
        string yKey,
        string zKey)
    {
        PlayerPrefs.SetFloat(xKey, value.x);
        PlayerPrefs.SetFloat(yKey, value.y);
        PlayerPrefs.SetFloat(zKey, value.z);
    }

    // 세 개의 PlayerPrefs 키에서 Vector3 값을 복원합니다
    private Vector3 LoadVector3(
        string xKey,
        string yKey,
        string zKey)
    {
        return new Vector3(
            PlayerPrefs.GetFloat(xKey, 0f),
            PlayerPrefs.GetFloat(yKey, 0f),
            PlayerPrefs.GetFloat(zKey, 0f));
    }

    // 저장된 체크포인트 번호를 반환합니다
    public int LoadCheckpointNumber()
    {
        return PlayerPrefs.GetInt(NumberKey, -1);
    }

    // 저장된 체크포인트 Scene Path를 반환합니다
    public string LoadScenePath()
    {
        return PlayerPrefs.GetString(
            ScenePathKey,
            string.Empty);
    }

    // 저장된 체크포인트 위치를 반환합니다
    public Vector3 LoadRespawnPosition()
    {
        return LoadVector3(
            PositionXKey,
            PositionYKey,
            PositionZKey);
    }

    // 저장된 플레이어 체력을 반환합니다
    public float LoadSavedHP()
    {
        return PlayerPrefs.GetFloat(HpKey, 0f);
    }

    // 저장된 회복 아이템 수량을 반환합니다
    public int LoadSavedHealItemCount()
    {
        return PlayerPrefs.GetInt(
            HealItemCountKey,
            0);
    }

    // 저장된 체크포인트 데이터를 모두 삭제합니다
    public void DeleteCheckpointSave()
    {
        PlayerPrefs.DeleteKey(HasSaveKey);
        PlayerPrefs.DeleteKey(IdKey);
        PlayerPrefs.DeleteKey(NumberKey);
        PlayerPrefs.DeleteKey(ScenePathKey);
        PlayerPrefs.DeleteKey(PositionXKey);
        PlayerPrefs.DeleteKey(PositionYKey);
        PlayerPrefs.DeleteKey(PositionZKey);
        PlayerPrefs.DeleteKey(RotationXKey);
        PlayerPrefs.DeleteKey(RotationYKey);
        PlayerPrefs.DeleteKey(RotationZKey);
        PlayerPrefs.DeleteKey(HpKey);
        PlayerPrefs.DeleteKey(HealItemCountKey);
        PlayerPrefs.Save();
    }
}
