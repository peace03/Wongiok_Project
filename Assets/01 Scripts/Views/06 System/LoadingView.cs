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

    protected override void Awake()
    {
        base.Awake();
        SubscribeEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    private void Update()
    {
        if (!isSpinning || spinningIcon == null)
            return;

        spinningIcon.Rotate(0f, 0f, -spinSpeed * Time.unscaledDeltaTime);
    }

    protected override void OnShow()
    {
        ResetLoadingView();
        isSpinning = true;
    }

    protected override void OnHide()
    {
        isSpinning = false;
        ResetLoadingView();
    }

    public void SetMessage(string message)
    {
        if (messageText == null)
            return;

        messageText.text = string.IsNullOrEmpty(message) ? defaultMessage : message;
    }

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

    public void ResetLoadingView()
    {
        SetMessage(defaultMessage);
        SetProgress(0f);

        if (spinningIcon != null)
        {
            spinningIcon.localRotation = Quaternion.identity;
        }
    }

    private void SubscribeEvents()
    {
        EventBus<UISetLoadingProgressEvent>.action += HandleSetLoadingProgress;
    }

    private void UnsubscribeEvents()
    {
        EventBus<UISetLoadingProgressEvent>.action -= HandleSetLoadingProgress;
    }

    private void HandleSetLoadingProgress(UISetLoadingProgressEvent eventData)
    {
        if (!string.IsNullOrEmpty(eventData.Message))
        {
            SetMessage(eventData.Message);
        }

        SetProgress(eventData.Progress);
    }
}
