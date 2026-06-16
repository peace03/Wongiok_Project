using UnityEngine;

public class Test_Y : MonoBehaviour
{
    private bool canParry;
    private void OnEnable()
    {
        EventBus<ParryEvent>.action += SetCanParry;
        EventBus<UltimateInvoke>.action += SetFalseParry;
    }
    private void OnDisable()
    {
        EventBus<ParryEvent>.action -= SetCanParry;
        EventBus<UltimateInvoke>.action += SetFalseParry;
    }
    public void SetCanParry(ParryEvent data)
    {
        canParry = data.CanParry;
        //Debug.Log(canParry);
    }
    public void SetFalseParry(UltimateInvoke data) 
    { 
        canParry = false;
        //Debug.Log($"canParry: {canParry}");
    }
}
