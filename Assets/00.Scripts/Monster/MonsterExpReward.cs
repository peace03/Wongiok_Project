using UnityEngine;

public class MonsterExpReward : MonoBehaviour
{
    [Header("Experience Reward")]
    // 이 몬스터가 사망했을 때 플레이어에게 지급할 경험치입니다.
    [SerializeField] private float expReward = 10f;

    public float ExpReward => Mathf.Max(0f, expReward);
}
