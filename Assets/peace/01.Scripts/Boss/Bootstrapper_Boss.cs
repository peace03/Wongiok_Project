using UnityEngine;

public class Bootstrapper_Boss : MonoBehaviour
{
    [SerializeField] private BossStatus bossStat;
    private void Awake()
    {
        bossStat.Init_Awake();
        bossStat.TestPrint();
    }
    private void Start()
    {

    }
    private void FixedUpdate()
    {

    }
    private void Update()
    {

    }
    private void LateUpdate()
    {

    }
}
