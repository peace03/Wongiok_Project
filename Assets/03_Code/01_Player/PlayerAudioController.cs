using UnityEngine;

public class PlayerAudioController : MonoBehaviour
{
    private static readonly int PistolRunState = Animator.StringToHash("Base Layer.Pistol Run");

    [Header("Attack Audio")]
    [SerializeField] private AudioClip fireSound;
    [SerializeField] private AudioClip bulletHitSound;

    [Header("Movement Audio")]
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip landingSound;
    [SerializeField] private AudioClip slideSound;
    [SerializeField] private AudioClip walkSound;
    [SerializeField, Range(0f, 0.49f)] private float firstFootstepNormalizedTime = 0.15f;

    private AudioSource audioSource;
    private Animator animator;
    private bool isWalking;
    private int lastFootstepIndex;
    private float lastWalkNormalizedTime;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
        animator = GetComponentInChildren<Animator>(true);
        ResetWalkTiming();
    }

    private void Update()
    {
        UpdateWalkSound();
    }

    private void OnEnable()
    {
        EventBus<PlayerAttackFiredEvent>.action += OnPlayerAttackFired;
        EventBus<PlayerBulletHitEvent>.action += OnPlayerBulletHit;
    }

    private void OnDisable()
    {
        EventBus<PlayerAttackFiredEvent>.action -= OnPlayerAttackFired;
        EventBus<PlayerBulletHitEvent>.action -= OnPlayerBulletHit;
        StopWalking();
    }

    public void PlayJump()
    {
        PlayOneShot(jumpSound);
    }

    public void PlayLanding()
    {
        PlayOneShot(landingSound);
    }

    public void PlaySlide()
    {
        PlayOneShot(slideSound);
    }

    public void StartWalking()
    {
        isWalking = true;
        ResetWalkTiming();
    }

    public void StopWalking()
    {
        isWalking = false;
        ResetWalkTiming();
    }

    private void UpdateWalkSound()
    {
        if (!isWalking || walkSound == null || !TryGetPistolRunNormalizedTime(out float normalizedTime))
            return;

        if (lastWalkNormalizedTime < 0f || normalizedTime < lastWalkNormalizedTime)
        {
            lastWalkNormalizedTime = normalizedTime;
            lastFootstepIndex = GetFootstepIndex(normalizedTime);
            return;
        }

        int footstepIndex = GetFootstepIndex(normalizedTime);
        if (footstepIndex > lastFootstepIndex)
        {
            PlayOneShot(walkSound);
            lastFootstepIndex = footstepIndex;
        }

        lastWalkNormalizedTime = normalizedTime;
    }

    private bool TryGetPistolRunNormalizedTime(out float normalizedTime)
    {
        normalizedTime = 0f;
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);
        if (animator == null || animator.runtimeAnimatorController == null)
            return false;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (animator.IsInTransition(0))
        {
            AnimatorStateInfo nextStateInfo = animator.GetNextAnimatorStateInfo(0);
            if (nextStateInfo.fullPathHash == PistolRunState)
                stateInfo = nextStateInfo;
        }

        if (stateInfo.fullPathHash != PistolRunState)
            return false;

        normalizedTime = stateInfo.normalizedTime;
        return true;
    }

    private int GetFootstepIndex(float normalizedTime)
    {
        return Mathf.FloorToInt((normalizedTime - firstFootstepNormalizedTime) * 2f);
    }

    private void ResetWalkTiming()
    {
        lastFootstepIndex = -1;
        lastWalkNormalizedTime = -1f;
    }

    private void OnPlayerAttackFired(PlayerAttackFiredEvent eventData)
    {
        PlayOneShot(fireSound);
    }

    private void OnPlayerBulletHit(PlayerBulletHitEvent eventData)
    {
        if (bulletHitSound != null)
            AudioSource.PlayClipAtPoint(bulletHitSound, eventData.HitPoint);
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (clip == null) return;

        if (audioSource != null)
            audioSource.PlayOneShot(clip);
        else
            AudioSource.PlayClipAtPoint(clip, transform.position);
    }
}
