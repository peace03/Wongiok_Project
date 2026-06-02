using UnityEngine;

public class BossController : MonoBehaviour
{
    private BossState curState;
    private Transform player;

    public void ChangeState(BossState state)
    {
        curState = state;
    }
}
