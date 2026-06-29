using UnityEngine;

public class PlayerStatus_Y : MonoBehaviour
{
    [SerializeField] private Stat_Y maxHP; //최대 체력
    private float curHP = 120;

    public void TakeDamage(float amount)
    {
        curHP -= amount;
        Debug.Log($"Player 현재 체력: {curHP}");
    }
}
