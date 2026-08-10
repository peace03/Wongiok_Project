using UnityEngine;
using UnityEngine.UI;

public class LoadingView : UIViewBase
{
    [Header("Loading Text")]
    [SerializeField] private Text messageText;
    [SerializeField] private Text progressText;

    [Header("Loading Visual")]
    [SerializeField] private Image progressFillImage;
    [SerializeField] private RectTransform spinningIcon;

    [Header("Settings")]
    [SerializeField] private string defaultMessage = "불러오는 중...";
    [SerializeField] private float spinSpeed = 180f;

    private bool isSpinning;

    // 2026.08.10_UI 정리: 컴포넌트 초기화와 이벤트 구독을 준비한다.
    protected override void Awake()
    {
        base.Awake();
    }

    // 2026.08.10_UI 정리: 프레임 단위 UI 상태와 입력을 갱신한다.
    private void Update()
    {
        if (!isSpinning || spinningIcon == null)
            return;

        spinningIcon.Rotate(0f, 0f, -spinSpeed * Time.unscaledDeltaTime);
    }

    // 2026.08.10_UI 정리: 화면 표시 시 필요한 UI 상태를 초기화한다.
    protected override void OnShow()
    {
        ResetLoadingView();
        isSpinning = true;
    }

    // 2026.08.10_UI 정리: 화면 숨김 시 임시 UI 상태를 정리한다.
    protected override void OnHide()
    {
        isSpinning = false;
        ResetLoadingView();
    }

    // 2026.08.10_UI 정리: 메시지 표시 값을 반영한다.
    public void SetMessage(string message)
    {
        if (messageText == null)
            return;

        messageText.text = string.IsNullOrEmpty(message) ? defaultMessage : message;
    }

    // 2026.08.10_UI 정리: 진행도 표시 값을 반영한다.
    public void SetProgress(float progress)
    {
        float clampedProgress = Mathf.Clamp01(progress);

        if (progressFillImage != null)
        {
            progressFillImage.fillAmount = clampedProgress;
        }

        if (progressText != null)
        {
            int percent = Mathf.RoundToInt(clampedProgress * 100f);
            progressText.text = $"{percent}%";
        }
    }

    // 2026.08.10_UI 정리: 로딩 View 상태를 기본값으로 초기화한다.
    public void ResetLoadingView()
    {
        SetMessage(defaultMessage);
        SetProgress(0f);

        if (spinningIcon != null)
        {
            spinningIcon.localRotation = Quaternion.identity;
        }
    }
}
