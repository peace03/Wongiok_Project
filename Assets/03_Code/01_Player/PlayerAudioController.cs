using UnityEngine;

public class PlayerAudioController : MonoBehaviour
{
    [Header("Attack Audio")]
    // 플레이어가 공격을 발사할 때 재생할 사운드입니다.
    [SerializeField] private AudioClip fireSound;

    // 총알이 무언가에 맞았을 때 재생할 사운드입니다.
    [SerializeField] private AudioClip bulletHitSound;

    // 이 오브젝트에 붙은 AudioSource입니다. 없으면 위치 기반 재생으로 대체합니다.
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        // 공격 발사와 총알 충돌 이벤트를 구독해 오디오만 분리해서 처리합니다.
        EventBus<PlayerAttackFiredEvent>.action += OnPlayerAttackFired;
        EventBus<PlayerBulletHitEvent>.action += OnPlayerBulletHit;
    }

    private void OnDisable()
    {
        // 오브젝트가 꺼질 때 반드시 구독을 해제해 중복 호출과 메모리 참조 문제를 방지합니다.
        EventBus<PlayerAttackFiredEvent>.action -= OnPlayerAttackFired;
        EventBus<PlayerBulletHitEvent>.action -= OnPlayerBulletHit;
    }

    private void OnPlayerAttackFired(PlayerAttackFiredEvent eventData)
    {
        // 발사 사운드는 플레이어 위치의 AudioSource를 우선 사용합니다.
        PlayOneShot(fireSound);
    }

    private void OnPlayerBulletHit(PlayerBulletHitEvent eventData)
    {
        if (bulletHitSound == null) return;

        // 피격 사운드는 충돌 위치에서 재생해 공간감을 줍니다.
        AudioSource.PlayClipAtPoint(bulletHitSound, eventData.HitPoint);
    }

    private void PlayOneShot(AudioClip clip)
    {
        // 클립이 비어 있으면 아무 소리도 재생하지 않습니다.
        if (clip == null) return;

        if (audioSource != null)
            audioSource.PlayOneShot(clip);
        else
            // AudioSource가 없더라도 사운드는 들리도록 현재 위치에서 임시 재생합니다.
            AudioSource.PlayClipAtPoint(clip, transform.position);
    }
}
