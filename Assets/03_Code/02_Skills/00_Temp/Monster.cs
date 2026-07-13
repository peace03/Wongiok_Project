using UnityEngine;

public class Monster : MonoBehaviour, IDamageable
{
    public float Hp = 100f;

    public void TakeDamage(float damage)
    {
        Hp = Mathf.Max(0f, Hp - damage);
        Debug.Log($"[Monster] HP 감소 => 현재 HP : {Hp}", this);

        if (Hp <= 0f)
            gameObject.SetActive(false);
    }
}