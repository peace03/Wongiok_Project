using UnityEngine;
using System.Collections.Generic;

public class AttackColliders : MonoBehaviour
{
    [SerializeField] private List<BoxCollider> attackColliders;
    [SerializeField] private List<Vector3> defaultPos; //?쇱そ 諛붾씪蹂대뒗 湲곗?

    private void OnEnable()
    {
        EventBus<ColliderToggleEvent>.action += ToggleCollider;
        EventBus<BossFacingChangeEvent>.action += ChangeColliderPos;
        EventBus<ParryKeyDown>.action += OffCollider;
    }
    private void OnDisable()
    {
        EventBus<ColliderToggleEvent>.action -= ToggleCollider;
        EventBus<BossFacingChangeEvent>.action -= ChangeColliderPos;
        EventBus<ParryKeyDown>.action -= OffCollider;
    }

    public void ToggleCollider(ColliderToggleEvent data)
    {
        attackColliders[(int)data.Type].enabled = data.IsEnabled;
    }
    public void OffCollider(ParryKeyDown data)
    {
        foreach(var collider in attackColliders)
            collider.enabled = false;
    }

    public void ChangeColliderPos(BossFacingChangeEvent data)
    {
        for (int i = 0; i < attackColliders.Count; i++)
        {
            if (data.Direction == BossFacing.Left) attackColliders[i].center = defaultPos[i];
            else attackColliders[i].center = -defaultPos[i];
        }
    }
}
