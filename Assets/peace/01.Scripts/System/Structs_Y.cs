//패링 키 눌렀음 이벤트
public struct ParryKeyDown { }

public struct HitStopEvent
{
    public int frames { get; private set; } //정지할 프레임 수
    public HitStopEvent(int frames)
    {
        this.frames = frames;
    }
}

public struct CameraShakeEvent
{
    public float impulseForce { get; private set; } //진동 강도
    public CameraShakeEvent(float force)
    {
        this.impulseForce = force;
    }
}