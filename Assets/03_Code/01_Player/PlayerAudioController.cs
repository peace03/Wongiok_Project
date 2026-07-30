using UnityEngine;

public class PlayerAudioController : MonoBehaviour
{
    [Header("Attack Audio")]
    // 플레이어가 공격을 발사할 때 재생할 사운드입니다.
    [SerializeField] private AudioClip fireSound;
        // 총알이 무언가에 맞았을 때 재생할 사운드입니다.
    [SerializeField] private AudioClip bulletHitSound;

    [Header("Movement Sound")]
    [SerializeField] private AudioClip[] footstepSounds;
    [SerializeField] private AudioClip slideSound;

    // 공격음과 이동음이 서로 중단되지 않도록 AudioSource를 분리해서 사용합니다.
    private AudioSource audioSource;
    private AudioSource movementAudioSource;

    private void Awake()
    {
        PrepareAudioClips();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        movementAudioSource = gameObject.AddComponent<AudioSource>();

        ConfigureAudioSource(audioSource);
        ConfigureAudioSource(movementAudioSource);
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
        StopWalkLoop();
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
    public void StartWalkLoop()
    {
        if (movementAudioSource == null ||
            footstepSounds == null ||
            footstepSounds.Length == 0)
            return;

        int index = Random.Range(0, footstepSounds.Length);
        AudioClip walkClip = footstepSounds[index];
        if (walkClip == null)
            return;

        EnsureAudioDataLoaded(walkClip);

        if (movementAudioSource.isPlaying &&
            movementAudioSource.loop &&
            movementAudioSource.clip == walkClip)
            return;

        movementAudioSource.Stop();
        movementAudioSource.clip = walkClip;
        movementAudioSource.loop = true;
        movementAudioSource.Play();
    }

    public void StopWalkLoop()
    {
        if (movementAudioSource == null || !movementAudioSource.loop)
            return;

        movementAudioSource.Stop();
        movementAudioSource.clip = null;
        movementAudioSource.loop = false;
    }

    public void PlaySlide()
    {
        if (movementAudioSource == null || slideSound == null)
            return;

        EnsureAudioDataLoaded(slideSound);

        movementAudioSource.Stop();
        movementAudioSource.clip = null;
        movementAudioSource.loop = false;
        movementAudioSource.PlayOneShot(slideSound);
    }

    private static void ConfigureAudioSource(AudioSource source)
    {
        source.playOnAwake = false;
        source.loop = false;
        source.mute = false;
        source.volume = 1f;
        source.spatialBlend = 0f;
    }

    private void PrepareAudioClips()
    {
        if (footstepSounds != null)
        {
            foreach (AudioClip footstepSound in footstepSounds)
                EnsureAudioDataLoaded(footstepSound);
        }

        EnsureAudioDataLoaded(slideSound);
    }

    private static void EnsureAudioDataLoaded(AudioClip clip)
    {
        if (clip != null && clip.loadState == AudioDataLoadState.Unloaded)
            clip.LoadAudioData();
    }
}
