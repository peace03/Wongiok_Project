using UnityEngine;

public class DeadZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider obj)
    {
        obj?.GetComponent<IDamageable>().TakeDamage(999);
    }
}
