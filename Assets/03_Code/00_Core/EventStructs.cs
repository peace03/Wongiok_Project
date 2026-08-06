using UnityEngine;
using System.Collections.Generic;

#region 패링 키 입력 이벤트
public struct ParryKeyDown { }
#endregion

#region 히트스탑 이벤트: 프레임 단위 정지
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
#endregion

#region 불릿타임 이벤트: 실시간 duration 동안 전역 시간 배율 변경
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
#endregion

#region 시간 제어
// 시간 연출 요청이 어디서 왔는지 구분한다. 충돌 정책을 사람이 읽기 쉽게 만들기 위한 태그다.
public enum TimeEffectSource { None, Telegraph, Parry, Impact, Skill } //Impact는 또다른 연출 사용시

// 같은 순간 여러 시간 연출이 들어왔을 때 어떤 연출을 남길지 결정하는 우선순위다.
public enum TimeEffectPriority { Low = 10, Medium = 50, High = 100 }

// 같은 그룹 안에서는 하나의 시간 연출만 허용한다. 전투 손맛 연출끼리 겹치는 문제를 막는다.
public static class TimeEffectGroups
{
    public const string CombatFeel = "CombatFeel";
}
#endregion

#region 카메라 쉐이크
public struct CameraShakeEvent
{
    public float impulseForce { get; private set; } // 진동 강도
    public CameraShakeEvent(float force) { this.impulseForce = force; }
}
#endregion

#region SFX
//2D 사운드 재생 요청
public readonly struct Play2DSoundEvent
{
    public readonly AudioClip Clip; //재생 오디오 클립
    public readonly List<AudioClip> Clips; //여러개 랜덤으로 재생할 오디오 클립
    public readonly float Volume; //0~1 재생 볼륨
    public Play2DSoundEvent(AudioClip clip = null, List<AudioClip> clips = null, float volume = 1f)
    {
        Clip = clip;
        Clips = clips;
        Volume = volume;
    }
}

// 재생 중인 특정 SFX를 나중에 중지하거나 페이드 아웃해야 할 때 사용하는 요청입니다.
// 차징음, 지속 기합음, 장판음처럼 PlayOneShot으로 제어할 수 없는 사운드에 사용합니다.
public readonly struct StartControlledSfxEvent
{
    public readonly string SoundInstanceId;
    public readonly AudioClip Clip;
    public readonly float Volume;
    public readonly bool Loop;

    public StartControlledSfxEvent(
        string soundInstanceId,
        AudioClip clip,
        float volume = 1f,
        bool loop = true)
    {
        SoundInstanceId = soundInstanceId;
        Clip = clip;
        Volume = volume;
        Loop = loop;
    }
}

// StartControlledSfxEvent로 시작한 특정 SFX를 종료하는 요청입니다.
public readonly struct StopControlledSfxEvent
{
    public readonly string SoundInstanceId;
    public readonly float FadeOutDuration;

    public StopControlledSfxEvent(
        string soundInstanceId,
        float fadeOutDuration = 0f)
    {
        SoundInstanceId = soundInstanceId;
        FadeOutDuration = fadeOutDuration;
    }
}

//BGM 재생 요청 (페이드 아웃 -> 페이드 인)
public readonly struct PlayBgmEvent
{
    public readonly AudioClip Clip;
    public readonly float Volume;
    public readonly float FadeDuration;
    public PlayBgmEvent(AudioClip clip, float volume = 1f, float fadeDuration = 1f)
    {
        Clip = clip;
        Volume = volume;
        FadeDuration = fadeDuration;
    }
}

//BGM 종료 요청
public readonly struct StopBgmEvent
{
    public readonly float FadeDuration;
    public StopBgmEvent(float fadeDuration = 1f) { FadeDuration = fadeDuration; }
}
#endregion
