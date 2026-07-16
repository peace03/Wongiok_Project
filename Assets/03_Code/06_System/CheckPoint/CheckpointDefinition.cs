using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(
    fileName = "CheckpointDefinition",
    menuName = "GRIMOIRE/Checkpoint/Checkpoint Definition")]
public class CheckpointDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string checkpointId;
    [SerializeField] private int displayNumber = 1;

    [Header("Destination")]
#if UNITY_EDITOR
    [SerializeField] private SceneAsset sceneAsset;
#endif
    [SerializeField] private string scenePath;
    [SerializeField] private Vector3 respawnPosition;
    [SerializeField] private Vector3 respawnEulerAngles;

    [Header("Recovery")]
    [SerializeField] private bool restoreHealth = true;
    [SerializeField] private bool restoreLives = true;
    [SerializeField] private bool restoreHealItems;

    public string CheckpointId => checkpointId;
    public int DisplayNumber => displayNumber;
    public string ScenePath => scenePath;
    public Vector3 RespawnPosition => respawnPosition;
    public Vector3 RespawnEulerAngles => respawnEulerAngles;
    public Quaternion RespawnRotation =>
        Quaternion.Euler(respawnEulerAngles);
    public bool RestoreHealth => restoreHealth;
    public bool RestoreLives => restoreLives;
    public bool RestoreHealItems => restoreHealItems;

    public bool IsValid =>
        !string.IsNullOrWhiteSpace(checkpointId) &&
        !string.IsNullOrWhiteSpace(scenePath);

#if UNITY_EDITOR
    // Scene Asset의 경로를 런타임에서 사용할 문자열로 저장합니다
    private void OnValidate()
    {
        displayNumber = Mathf.Max(0, displayNumber);

        if (sceneAsset != null)
        {
            scenePath = AssetDatabase.GetAssetPath(sceneAsset);
        }
    }

    // 씬에 배치한 부활 위치와 회전을 정의 에셋에 기록합니다
    public void BakeDestination(
        string bakedScenePath,
        Vector3 bakedPosition,
        Vector3 bakedEulerAngles)
    {
        Undo.RecordObject(this, "Bake Checkpoint Destination");

        scenePath = bakedScenePath;
        respawnPosition = bakedPosition;
        respawnEulerAngles = bakedEulerAngles;

        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
    }
#endif
}
