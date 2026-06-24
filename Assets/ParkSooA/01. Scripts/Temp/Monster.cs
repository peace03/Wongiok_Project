using UnityEngine;

public class Monster : MonoBehaviour, IDamageable
{
    public float Hp = 100f;

    public void TakeDamage(float damage)
    {
        Hp = Mathf.Max(0f, Hp - damage);

        if (Hp <= 0f)
            gameObject.SetActive(false);
    }
}