// 패링 키 입력 이벤트
public struct ParryKeyDown { }

// 히트스탑 이벤트: 프레임 단위 정지
public struct HitStopEvent
{
    public int frames { get; private set; } // 정지할 프레임 수
    public TimeEffectSource source { get; private set; }
    public TimeEffectPriority priority { get; private set; }
    public string exclusiveGroup { get; private set; }

    // 패링 히트스탑은 플레이어 입력 보상이므로 기본 우선순위를 가장 높게 둔다.
    public HitStopEvent(int frames)
        : this(frames, TimeEffectSource.Parry, TimeEffectPriority.High, TimeEffectGroups.CombatFeel) { }

    public HitStopEvent(int frames, TimeEffectSource source, TimeEffectPriority priority, string exclusiveGroup)
    {
        this.frames = frames;
        this.source = source;
        this.priority = priority;
        this.exclusiveGroup = exclusiveGroup;
    }
}

// 불릿타임 이벤트: 실시간 duration 동안 전역 시간 배율 변경
public struct SlowMoEvent
{
    public float targetScale { get; private set; }
    public float durationRealtime { get; private set; }
    public TimeEffectSource source { get; private set; }
    public TimeEffectPriority priority { get; private set; }
    public string exclusiveGroup { get; private set; }

    // 사전신호 불릿타임은 보조 연출이므로 기본 우선순위를 낮게 둔다.
    public SlowMoEvent(float scale, float duration)
        : this(scale, duration, TimeEffectSource.Telegraph, TimeEffectPriority.Low, TimeEffectGroups.CombatFeel) { }

    public SlowMoEvent(float scale, float duration, TimeEffectSource source, TimeEffectPriority priority, string exclusiveGroup)
    {
        this.targetScale = scale;
        this.durationRealtime = duration;
        this.source = source;
        this.priority = priority;
        this.exclusiveGroup = exclusiveGroup;
    }
}

// 시간 연출 요청이 어디서 왔는지 구분한다. 충돌 정책을 사람이 읽기 쉽게 만들기 위한 태그다.
public enum TimeEffectSource { None, Telegraph, Parry, Impact } //Impact는 또다른 연출 사용시

// 같은 순간 여러 시간 연출이 들어왔을 때 어떤 연출을 남길지 결정하는 우선순위다.
public enum TimeEffectPriority { Low = 10, Medium = 50, High = 100 }

// 같은 그룹 안에서는 하나의 시간 연출만 허용한다. 전투 손맛 연출끼리 겹치는 문제를 막는다.
public static class TimeEffectGroups
{
    public const string CombatFeel = "CombatFeel";
}

public struct CameraShakeEvent
{
    public float impulseForce { get; private set; } // 진동 강도
    public CameraShakeEvent(float force) { this.impulseForce = force; }
}
