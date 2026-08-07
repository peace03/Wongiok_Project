using System;

// UI 초기화 이벤트입니다.
// 게임 전체 Reset의 Single Entry Point에서 발행해 UIManager.ResetUI()를 호출하게 만드는 용도입니다.
public struct UIResetEvent
{

}

public struct UIFadeEvent
{
    public float FromAlpha { get; private set; }
    public float ToAlpha { get; private set; }
    public float Duration { get; private set; }
    public Action OnComplete { get; private set; }

    public UIFadeEvent(
        float fromAlpha,
        float toAlpha,
        float duration,
        Action onComplete = null)
    {
        FromAlpha = fromAlpha;
        ToAlpha = toAlpha;
        Duration = duration;
        OnComplete = onComplete;
    }
}

// 2026.08.07_psb수정
// 영상 View가 첫 프레임을 출력할 준비를 마쳤음을 전환 담당자에게 알린다.
public struct UIVideoFirstFrameReadyEvent
{
    public string VideoId { get; private set; }

    public UIVideoFirstFrameReadyEvent(string videoId)
    {
        VideoId = videoId;
    }
}
