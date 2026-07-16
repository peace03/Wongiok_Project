using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class CheckpointDefinitionBinder : MonoBehaviour
{
    [Header("Definition")]
    [SerializeField] private CheckpointDefinition definition;

    [Header("Scene Reference")]
    [SerializeField] private Transform respawnPoint;

    public CheckpointDefinition Definition => definition;
    public Transform RespawnPoint => respawnPoint;

    // 처음 추가될 때 RespawnPoint 이름의 자식 오브젝트를 자동으로 찾습니다
    private void Reset()
    {
        Transform[] children =
            GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name != "RespawnPoint")
            {
                continue;
            }

            respawnPoint = children[i];
            break;
        }
    }

#if UNITY_EDITOR
    // 현재 씬의 RespawnPoint 위치와 회전을 Definition에 기록합니다
    [ContextMenu("Bake Respawn Point To Definition")]
    private void BakeRespawnPointToDefinition()
    {
        if (definition == null)
        {
            Debug.LogWarning(
                "CheckpointDefinition이 연결되지 않았습니다.",
                this);
            return;
        }

        Transform source =
            respawnPoint != null ? respawnPoint : transform;

        string scenePath = gameObject.scene.path;

        if (string.IsNullOrWhiteSpace(scenePath))
        {
            Debug.LogWarning(
                "저장되지 않은 씬에서는 체크포인트를 Bake할 수 없습니다.",
                this);
            return;
        }

        definition.BakeDestination(
            scenePath,
            source.position,
            source.eulerAngles);

        Debug.Log(
            $"Checkpoint Bake 완료: {definition.CheckpointId}",
            definition);

        EditorUtility.SetDirty(this);
    }
#endif
}
