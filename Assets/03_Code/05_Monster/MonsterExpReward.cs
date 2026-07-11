using UnityEngine;

public class MonsterExpReward : MonoBehaviour
{
    [SerializeField] private float expReward = 10f;

    public float ExpReward => Mathf.Max(0f, expReward);
}
