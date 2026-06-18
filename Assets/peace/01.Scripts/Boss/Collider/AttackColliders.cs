using UnityEngine;
using System.Collections.Generic;

public class AttackColliders : MonoBehaviour
{
    [SerializeField] private List<BoxCollider> attackColliders;
    [SerializeField] private List<Vector3> defaultPos; //왼쪽 바라보는 기준

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
        attackColliders[(int)data.type].enabled = data.state;
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
            if (data.dir == Facing.Left) attackColliders[i].center = defaultPos[i];
            else attackColliders[i].center = -defaultPos[i];
        }
    }
}