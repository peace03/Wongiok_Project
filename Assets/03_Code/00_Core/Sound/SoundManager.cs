using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

//EventBus에서 사운드 요청 수신
public class SoundManager : MonoBehaviour, IInitializable
{
    public int Priority => (int)InitOrder.System + 2;

    #region 인스펙터 / 변수
    [Header("2D SFX")]
    [SerializeField] private AudioSource sfx2DSource; //2D 사운드를 실제로 출력할 AudioSource
    [SerializeField] private AudioMixerGroup sfxMixerGroup; //오디오 채널 그룹 (SFX를 전부 같은 볼륨 정책으로 관리할 수 있음)

    [Header("BGM")]
    [SerializeField] private AudioSource bgmSourceA;
    [SerializeField] private AudioSource bgmSourceB; //교차 페이드를 위한 두번째 Source
    [SerializeField] private AudioMixerGroup bgmMixerGroup; //BGM 출력용 그룹
    [Tooltip("PlayBgmEvent에서 별도 시간을 주지 않았을 때 사용할 기본 페이드 시간")]
    [SerializeField] private float defaultBgmFadeDuration = 1f;

    private AudioSource activeBgmSource;    //마지막 재생 요청을 받은 BGM Source
    private Coroutine bgmFadeCoroutine;     //BGM 페이드 코루틴
    private bool isSubscribed;              //EventBus 중복 구독 방지용
    #endregion

    #region 초기화
    public void Init()
    {
        //AudioSource 참조를 준비하고 2D/BGM 재생 설정 적용
        if (!CacheAndConfigureSources()) return;
    }

    //AudioSource 캐싱, 역할에 맞는 기본 설정 적용
    private bool CacheAndConfigureSources()
    {
        //BGM Source 둘 중 하나라도 없으면 교차 페이드 불가능
        if (sfx2DSource == null || bgmSourceA == null || bgmSourceB == null)
        {
            Debug.LogError("[Sound] SFX Source와 BGM Source A/B를 모두 연결해야합니다.", this);
            return false;
        }

        ConfigureSfxSource();
        ConfigureBgmSource(bgmSourceA);
        ConfigureBgmSource(bgmSourceB);

        //시작시에는 BGM이 없는상태
        bgmSourceA.Stop();
        bgmSourceB.Stop();
        bgmSourceA.volume = 0f;
        bgmSourceB.volume = 0f;

        return true;
    }
    #endregion

    #region 활성/비활성
    private void OnEnable()
    {
        EventBus<Play2DSoundEvent>.action += HandlePlay2DSound;
        EventBus<PlayBgmEvent>.action += HandlePlayBgm;
        EventBus<StopBgmEvent>.action += HandleStopBgm;
    }
    private void OnDisable()
    {
        EventBus<Play2DSoundEvent>.action -= HandlePlay2DSound;
        EventBus<PlayBgmEvent>.action -= HandlePlayBgm;
        EventBus<StopBgmEvent>.action -= HandleStopBgm;
    }
    #endregion

    #region Source 설정
    //2D 단발 SFX Source 설정
    private void ConfigureSfxSource()
    {
        sfx2DSource.spatialBlend = 0f;
        sfx2DSource.loop = false;
        sfx2DSource.outputAudioMixerGroup = sfxMixerGroup;
    }

    //BGM Source 공통 설정
    private void ConfigureBgmSource(AudioSource source)
    {
        source.spatialBlend = 0f;
        source.loop = true;
        source.playOnAwake = false;
        source.outputAudioMixerGroup = bgmMixerGroup;
    }
    #endregion

    #region SFX 재생/종료
    //2D 단발 SFX 요청을 실제 재생
    private void HandlePlay2DSound(Play2DSoundEvent soundEvent)
    {
        if (soundEvent.Clip == null || sfx2DSource == null) return;
        //다른 SFX와 겹쳐서 재생
        sfx2DSource.PlayOneShot(soundEvent.Clip, Mathf.Clamp01(soundEvent.Volume));
    }

    //새 BGM 재생 요청 처리
    private void HandlePlayBgm(PlayBgmEvent bgmEvent)
    {
        if (bgmEvent.Clip == null) return;
        //같은 BGM이 재생중이면 다시 재생하지 않음
        if (activeBgmSource != null &&
            activeBgmSource.isPlaying &&
            activeBgmSource.clip == bgmEvent.Clip)
            return;

        //진행중인 이전 페이드 멈춤
        StopCurrentBgmFade();

        //다음 BGM Source 저장
        AudioSource nextBgmSource = GetNextBgmSource();

        //다음 BGM Source 새 클립 재생 상태로 준비
        nextBgmSource.Stop();
        nextBgmSource.clip = bgmEvent.Clip;
        nextBgmSource.volume = 0f;
        nextBgmSource.loop = true;
        nextBgmSource.Play();

        //0이하라면 기본 페이드 시간 사용
        float fadeDuration = (bgmEvent.FadeDuration > 0f) ? bgmEvent.FadeDuration : defaultBgmFadeDuration;

        //두 Source 교차 페이드
        bgmFadeCoroutine = StartCoroutine(
            CrossFadeBgmRoutine(nextBgmSource, Mathf.Clamp01(bgmEvent.Volume), fadeDuration));
    }

    //현재 BGM 페이드 아웃 종료
    private void HandleStopBgm(StopBgmEvent bgmEvent)
    {
        StopCurrentBgmFade();
        float fadeDuration = (bgmEvent.FadeDuration > 0f) ? bgmEvent.FadeDuration : defaultBgmFadeDuration;
        bgmFadeCoroutine = StartCoroutine(FadeOutAllBgmRoutine(fadeDuration));
    }
    #endregion

    //다음 BGM을 재생할 다음 Source 선택
    private AudioSource GetNextBgmSource()
    {
        //처음 BGM 재생하는 경우 A Source 사용
        if (activeBgmSource == null) return bgmSourceA;
        //다른 Source에 다음 BGM 사용
        return activeBgmSource == bgmSourceA ? bgmSourceB : bgmSourceA;
    }

    #region Etc
    //새 BGM 요청이 들어왔을 때 이전 페이드 코루틴 중단
    private void StopCurrentBgmFade()
    {
        if (bgmFadeCoroutine == null) return;
        StopCoroutine(bgmFadeCoroutine);
        bgmFadeCoroutine = null;
    }

    //기존 BGM 줄이고 새 BGM을 키우는 교차 페이드 코루틴
    private IEnumerator CrossFadeBgmRoutine(AudioSource nextBgmSource, float targetVolume, float fadeDuration)
    {
        float sourceAStartVolume = bgmSourceA.volume;
        float sourceBStartVolume = bgmSourceB.volume;

        //즉시 전환을 원하면 페이드 없이 바로 적용
        if (fadeDuration <= 0f)
        {
            FinishCrossFade(nextBgmSource, targetVolume);
            yield break;
        }

        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsedTime / fadeDuration);

            //다음 BGM은 0에서 목표 볼륨까지 커짐
            nextBgmSource.volume = Mathf.Lerp(0f, targetVolume, progress);
            //다음 BGM이 아닌 Source는 기존 볼륨에서 0까지 줄어듦
            if (bgmSourceA != nextBgmSource) bgmSourceA.volume = Mathf.Lerp(sourceAStartVolume, 0f, progress);
            if (bgmSourceB != nextBgmSource) bgmSourceB.volume = Mathf.Lerp(sourceBStartVolume, 0f, progress);

            yield return null;
        }

        FinishCrossFade(nextBgmSource, targetVolume);
    }

    //교차 페이드가 끝난 뒤 이전 BGM 정리
    private void FinishCrossFade(AudioSource nextBgmSource, float targetVolume)
    {
        nextBgmSource.volume = targetVolume;

        StopSourceIfNotNext(bgmSourceA, nextBgmSource);
        StopSourceIfNotNext(bgmSourceB, nextBgmSource);

        activeBgmSource = nextBgmSource;
        bgmFadeCoroutine = null;
    }

    //다음 BGM Source가 아닌 경우 재생을 중단하고 클립을 비움
    private void StopSourceIfNotNext(AudioSource source, AudioSource nextBgmSource)
    {
        if (source == nextBgmSource) return;
        source.Stop();
        source.clip = null;
        source.volume = 0f;
    }

    //현재 재생 중인 모든 BGM을 동시 에 페이드 아웃
    private IEnumerator FadeOutAllBgmRoutine(float fadeDuration)
    {
        float sourceAStartVolume = bgmSourceA.volume;
        float sourceBStartVolume = bgmSourceB.volume;

        //즉시 종료 요청이면 바로 정리
        if(fadeDuration <= 0f)
        {
            StopAllBgmSources();
            yield break;
        }

        float elapsedTime = 0f;
        while(elapsedTime < fadeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsedTime / fadeDuration);
            bgmSourceA.volume = Mathf.Lerp(sourceAStartVolume, 0f, progress);
            bgmSourceB.volume = Mathf.Lerp(sourceBStartVolume, 0f, progress);

            yield return null;
        }

        StopAllBgmSources();
    }

    //BGM Source 두개를 모두 중단하고 초기 상태로 되돌림
    private void StopAllBgmSources()
    {
        bgmSourceA.Stop();
        bgmSourceB.Stop();

        bgmSourceA.clip = null;
        bgmSourceB.clip = null;

        bgmSourceA.volume = 0f;
        bgmSourceB.volume = 0f;

        activeBgmSource = null;
        bgmFadeCoroutine = null;
    }
    #endregion
}

//호출 예시

//단발성 사운드 재생: 클립 볼륨 1로 재생
//EventBus<Play2DSoundEvent>.Publish(new Play2DSoundEvent(SFX2DClip, 1f));

// 보스전 시작: 1.5초 동안 페이드 인아웃, 보스 BGM으로 전환
//EventBus<PlayBgmEvent>.Publish(new PlayBgmEvent(bossBgmClip, 0.8f, 1.5f));

// 보스전 종료: 2초 동안 BGM 페이드 아웃
//EventBus<StopBgmEvent>.Publish(new StopBgmEvent(2f));
