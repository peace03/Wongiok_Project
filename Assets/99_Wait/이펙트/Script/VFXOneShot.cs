using UnityEngine;

public class VFXOneShot : MonoBehaviour
{
    [SerializeField] float lifetime = 0.5f;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }
}