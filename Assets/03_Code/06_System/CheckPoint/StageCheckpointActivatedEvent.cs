using UnityEngine;

public readonly struct StageCheckpointActivatedEvent
{
    public GameObject PlayerObject { get; }
    public GameObject CheckpointObject { get; }
    public CheckpointDefinition Definition { get; }

    // 체크포인트 활성화에 필요한 참조를 저장합니다
    public StageCheckpointActivatedEvent(
        GameObject playerObject,
        GameObject checkpointObject,
        CheckpointDefinition definition)
    {
        PlayerObject = playerObject;
        CheckpointObject = checkpointObject;
        Definition = definition;
    }
}
