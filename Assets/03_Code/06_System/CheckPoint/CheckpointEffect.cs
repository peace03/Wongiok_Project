using UnityEngine;

public class CheckpointEffect : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private Animator animator;
    [SerializeField]
    private string activateTriggerName = "Activate";
    [SerializeField]
    private ParticleSystem activationParticle;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip activationClip;

    private bool hasPlayed;

    // 체크포인트 연출에 필요한 참조를 준비합니다
    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    // 체크포인트 활성화 연출을 최초 한 번만 실행합니다
    public void PlayOnce()
    {
        if (hasPlayed)
        {
            return;
        }

        hasPlayed = true;
        PlayActivationEffect();
    }

    // 테스트 또는 스테이지 초기화를 위해 재생 상태를 초기화합니다
    public void ResetPlayedState()
    {
        hasPlayed = false;
    }

    // 체크포인트 애니메이션과 파티클 및 사운드를 실행합니다
    private void PlayActivationEffect()
    {
        if (animator != null &&
            !string.IsNullOrEmpty(activateTriggerName))
        {
            animator.SetTrigger(activateTriggerName);
        }

        if (activationParticle != null)
        {
            activationParticle.Play();
        }

        if (audioSource != null &&
            activationClip != null)
        {
            audioSource.PlayOneShot(activationClip);
        }
    }
}
