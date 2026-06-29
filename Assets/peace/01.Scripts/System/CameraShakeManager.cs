using UnityEngine;
using Unity.Cinemachine;

[RequireComponent(typeof(CinemachineImpulseSource))]
public class CameraShakeManager : MonoBehaviour, IInitializable
{
    public int Priority => (int)InitOrder.System + 1;



    public void Init()
    {
        throw new System.NotImplementedException();
    }
}
