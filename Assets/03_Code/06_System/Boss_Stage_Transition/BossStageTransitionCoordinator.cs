using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Video;

public class BossStageTransitionCoordinator : MonoBehaviour
{
    [Header("Arrival")]
    [SerializeField] private Transform playerArrivalPoint;
    [SerializeField] private SideViewCameraFollow sideViewCamera;
    [SerializeField] private Transform cameraArrivalPoint;

    [Header("Boss")]
    [SerializeField] private BossRuntimeActivator bossRuntimeActivator;

    [Header("Boss Intro")]
    [SerializeField] private bool playBossIntro = true;
    [SerializeField] private string bossIntroId = "chapter01_boss_intro";
    [SerializeField] private VideoClip bossIntroClip;
    [TextArea]
    [SerializeField] private string bossIntroSkipSummary;
    [SerializeField] private float introTimeoutSeconds = 120f;

    [Header("Timing")]
    [SerializeField] private float teleportSettleDelay = 0.1f;

    [Header("Events")]
    [SerializeField] private UnityEvent onTransitionStarted;
    [SerializeField] private UnityEvent onPlayerTeleported;
    [SerializeField] private UnityEvent onBossBattleStarted;

    private Coroutine transitionRoutine;
    private PlayerController activePlayerController;
    private bool previousPlayerControllerEnabled;
    private bool isWaitingForIntro;

    public bool IsTransitioning => transitionRoutine != null;

    // 보스 인트로 종료 이벤트를 구독합니다.
    private void OnEnable()
    {
        EventBus<UICutsceneFinishedEvent>.action +=
            HandleCutsceneFinished;
    }

    // 보스 인트로 종료 이벤트 구독을 해제하고 플레이어 제어를 복구합니다.
    private void OnDisable()
    {
        EventBus<UICutsceneFinishedEvent>.action -=
            HandleCutsceneFinished;

        RestorePlayerControl();
    }

    // 플레이어를 받아 보스 스테이지 전환을 한 번만 시작합니다.
    public bool TryBeginTransition(
        PlayerController playerController)
    {
        if (transitionRoutine != null || playerController == null)
        {
            return false;
        }

        if (playerArrivalPoint == null || bossRuntimeActivator == null)
        {
            Debug.LogError(
                "보스 스테이지 전환에 필요한 참조가 비어 있습니다.",
                this);
            return false;
        }

        transitionRoutine = StartCoroutine(
            RunTransitionRoutine(playerController));

        return true;
    }

    // 순간이동과 인트로 재생 후 보스전을 시작합니다.
    private IEnumerator RunTransitionRoutine(
        PlayerController playerController)
    {
        activePlayerController = playerController;
        previousPlayerControllerEnabled = playerController.enabled;

        onTransitionStarted?.Invoke();

        playerController.enabled = false;
        playerController.TeleportTo(playerArrivalPoint.position);
        Physics.SyncTransforms();

        SnapCameraToBossArea();
        onPlayerTeleported?.Invoke();

        if (teleportSettleDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(
                teleportSettleDelay);
        }

        if (ShouldPlayBossIntro())
        {
            yield return PlayBossIntroRoutine();
        }

        bossRuntimeActivator.ActivateAndInitialize();

        if (sideViewCamera != null)
        {
            sideViewCamera.Unlock();
        }

        RestorePlayerControl();
        onBossBattleStarted?.Invoke();

        transitionRoutine = null;
    }

    // 플레이어 순간이동 직후 기존 사이드뷰 카메라를 보스 구역에 맞춥니다.
    private void SnapCameraToBossArea()
    {
        if (sideViewCamera == null || cameraArrivalPoint == null)
        {
            return;
        }

        sideViewCamera.LockTo(cameraArrivalPoint);
        sideViewCamera.transform.SetPositionAndRotation(
            cameraArrivalPoint.position,
            cameraArrivalPoint.rotation);
    }

    // 현재 설정으로 보스 인트로 영상을 재생할 수 있는지 확인합니다.
    private bool ShouldPlayBossIntro()
    {
        if (!playBossIntro)
        {
            return false;
        }

        if (bossIntroClip != null)
        {
            return true;
        }

        Debug.LogWarning(
            "Boss Intro Clip이 비어 있어 인트로를 건너뜁니다.",
            this);
        return false;
    }

    // 기존 Cutscene UI에 보스 인트로 재생을 요청하고 종료를 기다립니다.
    private IEnumerator PlayBossIntroRoutine()
    {
        isWaitingForIntro = true;

        EventBus<UISetCutsceneEvent>.Publish(
            new UISetCutsceneEvent(
                bossIntroId,
                bossIntroClip,
                bossIntroSkipSummary));

        EventBus<UIOpenOverlayEvent>.Publish(
            new UIOpenOverlayEvent(
                UIOverlayState.Cutscene));

        float elapsedTime = 0f;

        while (isWaitingForIntro)
        {
            if (introTimeoutSeconds > 0f)
            {
                elapsedTime += Time.unscaledDeltaTime;

                if (elapsedTime >= introTimeoutSeconds)
                {
                    Debug.LogWarning(
                        "보스 인트로 종료 신호를 기다리다 제한 시간을 초과했습니다.",
                        this);
                    isWaitingForIntro = false;
                }
            }

            yield return null;
        }

        EventBus<UICloseOverlayEvent>.Publish(
            new UICloseOverlayEvent(
                UIOverlayState.Cutscene));
    }

    // 현재 재생 중인 보스 인트로와 ID가 일치하면 대기를 종료합니다.
    private void HandleCutsceneFinished(
        UICutsceneFinishedEvent eventData)
    {
        if (!isWaitingForIntro ||
            eventData.CutsceneId != bossIntroId)
        {
            return;
        }

        isWaitingForIntro = false;
    }

    // 전환 중 잠근 PlayerController의 기존 활성 상태를 복구합니다.
    private void RestorePlayerControl()
    {
        if (activePlayerController == null)
        {
            return;
        }

        activePlayerController.enabled =
            previousPlayerControllerEnabled;
        activePlayerController = null;
    }
}
