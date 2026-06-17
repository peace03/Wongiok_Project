using UnityEngine;
using System.Collections.Generic;

public class AttackColliders : MonoBehaviour
{
    [SerializeField] private List<BoxCollider> attackColliders;
    [SerializeField] private List<Vector3> defaultPos; //왼쪽 바라보는 기준

    private void OnEnable()
    {
        EventBus<ColliderEvent>.action += ToggleCollider;
        EventBus<BossFacingChangeEvent>.action += ChangeColliderPos;
    }
    private void OnDisable()
    {
        EventBus<ColliderEvent>.action -= ToggleCollider;
        EventBus<BossFacingChangeEvent>.action -= ChangeColliderPos;
    }

    public void ToggleCollider(ColliderEvent data)
    {
        attackColliders[(int)data.type].enabled = data.state;
    }

    public void ChangeColliderPos(BossFacingChangeEvent data)
    {
        for (int i = 0; i < attackColliders.Count; i++)
        {
            if (data.dir == Facing.Left) attackColliders[i].center = defaultPos[i];
            else attackColliders[i].center = -defaultPos[i];
        }
    }
}