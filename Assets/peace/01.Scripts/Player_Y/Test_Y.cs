using UnityEngine;

public class Test_Y : MonoBehaviour
{
    private bool canParry;
    private void OnEnable()
    {
        EventBus<CanParryEvent>.action += SetCanParry;
        EventBus<UltimateInvoke>.action += SetFalseParry;
    }
    private void OnDisable()
    {
        EventBus<CanParryEvent>.action -= SetCanParry;
        EventBus<UltimateInvoke>.action += SetFalseParry;
    }
    private void Update()
    {
        if (canParry == true && Input.GetKeyDown(KeyCode.E))
            EventBus<ParryKeyDown>.Publish(default);
    }
    public void SetCanParry(CanParryEvent data)
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
