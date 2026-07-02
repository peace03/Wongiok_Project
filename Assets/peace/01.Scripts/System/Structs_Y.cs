//패링 키 눌렀음 이벤트
public struct ParryKeyDown { }

//히트스탑(프레임 단위 정지)
public struct HitStopEvent
{
    public int frames { get; private set; } //정지할 프레임 수
    public HitStopEvent(int frames) { this.frames = frames; }
}

//불릿타임 (시간 단위 배율 둔화)
public struct SlowMoEvent
{
    public float targetScale { get; private set; }
    public float durationRealtime { get; private set; }
    public SlowMoEvent(float scale, float duration)
    {
        this.targetScale = scale;
        this.durationRealtime = duration;
    }
}

public struct CameraShakeEvent
{
    public float impulseForce { get; private set; } //진동 강도
    public CameraShakeEvent(float force) { this.impulseForce = force; }
}