using UnityEngine;

[CreateAssetMenu(
    fileName = "CheckpointCatalog",
    menuName = "GRIMOIRE/Checkpoint/Checkpoint Catalog")]
public class CheckpointCatalog : ScriptableObject
{
    [SerializeField]
    private CheckpointDefinition[] definitions;

    // 고유 ID와 일치하는 CheckpointDefinition을 찾습니다
    public bool TryFind(
        string checkpointId,
        out CheckpointDefinition definition)
    {
        definition = null;

        if (string.IsNullOrWhiteSpace(checkpointId) ||
            definitions == null)
        {
            return false;
        }

        for (int i = 0; i < definitions.Length; i++)
        {
            CheckpointDefinition candidate = definitions[i];

            if (candidate == null ||
                candidate.CheckpointId != checkpointId)
            {
                continue;
            }

            definition = candidate;
            return true;
        }

        return false;
    }
}
