using UnityEngine;

public class InGameBgmController : MonoBehaviour
{
    [SerializeField] private AudioClip fieldBgm;
    [SerializeField, Range(0f, 1f)] private float volume = 0.5f;

    private void Start()
    {
        if (fieldBgm == null)
            return;

        EventBus<PlayBgmEvent>.Publish(
            new PlayBgmEvent(fieldBgm, volume));
    }
}
