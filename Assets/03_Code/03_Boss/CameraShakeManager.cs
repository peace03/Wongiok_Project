using UnityEngine;
using Unity.Cinemachine;

[RequireComponent(typeof(CinemachineImpulseSource))]
public class CameraShakeManager : MonoBehaviour, IInitializable
{
    public int Priority => (int)InitOrder.System + 1;

    private CinemachineImpulseSource impulseSource;

    public void Init()
    {
        impulseSource = GetComponent<CinemachineImpulseSource>();
    }

    private void OnEnable()
    {
        EventBus<CameraShakeEvent>.action += OnCameraShake;
    }
    private void OnDisable()
    {
        EventBus<CameraShakeEvent>.action -= OnCameraShake;
    }

    private void OnCameraShake(CameraShakeEvent data)
    {
        impulseSource.GenerateImpulseWithForce(data.impulseForce);
    }
}
