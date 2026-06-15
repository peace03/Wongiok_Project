using UnityEngine;
using System.Collections.Generic;

public class AttackColliders : MonoBehaviour
{
    [SerializeField] private List<BoxCollider> attackColliders;

    private void OnEnable()
    {
        EventBus<ColliderEvent>.action += ToggleCollider;
    }
    private void OnDisable()
    {
        EventBus<ColliderEvent>.action -= ToggleCollider;
    }

    public void ToggleCollider(ColliderEvent data)
    {
        attackColliders[(int)data.type].enabled = data.state;
    }
}