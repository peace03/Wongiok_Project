using UnityEngine;

public class BossStatus : MonoBehaviour
{
    [SerializeField] private BossStatusData status;

    public void Init_Awake()
    {
        status.Init();
        status.ResetAllModifiers();
    }

    public void TestPrint()
    {
        Debug.Log(status.CurrentHP);
    }
}
