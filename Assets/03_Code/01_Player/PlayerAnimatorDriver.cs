using System.Collections;
using UnityEngine;

public sealed class PlayerAnimatorDriver : MonoBehaviour
{
    // Animator.StringToHash는 문자열을 매번 비교하지 않고 정수 ID로 바꿔 사용하기 위한 기능입니다.
    // 아래 값들은 애니메이션의 시간이나 속도가 아니라 Animator 파라미터와 State를 찾는 식별자입니다.
    private static readonly int IsMoving = Animator.StringToHash("IsMoving");
    private static readonly int IsFalling = Animator.StringToHash("IsFalling");
    private static readonly int IsExecutingSkill = Animator.StringToHash("IsExecutingSkill");
    private static readonly int ShootTrigger = Animator.StringToHash("Shoot");

    // "Base Layer.상태 이름"처럼 전체 경로를 사용하면 다른 레이어에 같은 이름의 State가 생겨도
    // 원하는 Base Layer의 State를 정확하게 지정할 수 있습니다.
    private static readonly int IdleState = Animator.StringToHash("Base Layer.Idle");
    private static readonly int PistolRunState = Animator.StringToHash("Base Layer.Pistol Run");
    private static readonly int JumpState = Animator.StringToHash("Base Layer.Jump");
    private static readonly int LandingState = Animator.StringToHash("Base Layer.Landing");
    private static readonly int SlidingState = Animator.StringToHash("Base Layer.Sliding");
    private static readonly int PistolShootState = Animator.StringToHash("Base Layer.Pistol Shoot");
    private static readonly int HitState = Animator.StringToHash("Base Layer.Hit");
    private static readonly int DeathState = Animator.StringToHash("Base Layer.Death");
    private static readonly int HitTrigger = Animator.StringToHash("Hit");
    private static readonly int DeathTrigger = Animator.StringToHash("Death");

    // Animator Controller의 Magnum/Rifle/Sniper 상태를 무기별 스킬 진입점으로 사용합니다.
    // 상태 이름과 Trigger 이름을 함께 Hash로 보관해 문자열 오타와 반복 변환을 방지합니다.
    private static readonly int MagnumSkillStartState = Animator.StringToHash("Base Layer.MagnumSkillStartState");
    private static readonly int MagnumSkillEndState = Animator.StringToHash("Base Layer.MagnumSkillEndState");
    private static readonly int RifleSkillState = Animator.StringToHash("Base Layer.RifleSkill");
    private static readonly int SniperSkillStartState = Animator.StringToHash("Base Layer.SniperSkillStartState");
    private static readonly int SniperSkillEndState = Animator.StringToHash("Base Layer.SniperSkillEndState");

    // CrossFadeInFixedTime의 두 번째 인자에 전달되는 블렌딩 시간이며 단위는 초입니다.
    // Animator Controller의 Transition Duration을 수정하는 값이 아니라,
    // 코드에서 State를 직접 재생할 때 사용할 전환 시간을 정해 둔 상수입니다.
    // 일반 이동은 입력 반응이 빨라야 하므로 짧게, Landing 보조 전환은 끊김을 줄이기 위해 조금 길게 사용합니다.
    private const float LocomotionBlendDuration = 0.05f;
    private const float LandingBlendDuration = 0.15f;

    private const int BaseLayerIndex = 0;

    // visualRoot는 캐릭터의 좌우 방향을 회전시키는 대상입니다.
    // animator는 실제 Animation State와 파라미터를 제어하는 컴포넌트입니다.
    // Inspector에서 직접 연결할 수 있고, 비어 있으면 ResolveReferences에서 자동으로 찾습니다.
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Animator animator;

    // 일부 애니메이션의 Root Transform 값이나 좌우 전환이 모델의 로컬 위치·회전을 누적해서
    // 모델 중심이 실제 Player 오브젝트에서 벗어나는 것을 막기 위한 기준값입니다.
    private Transform animatedModelRoot;
    private Vector3 animatedModelInitialLocalPosition;
    private Quaternion animatedModelInitialLocalRotation;
    private bool hasAnimatedModelAnchor;

    // 입력 시스템
    private GameInputReader inputReader = null;
    // 스킬 애니메이션 재생 코루틴
    private Coroutine skillCoroutine = null;
    // 스킬 시작 애니메이션 지속 시간
    private float skillStartAnimDuration = 0f;

    public float SkillStartAnimDuration => skillStartAnimDuration;
    public bool IsPlayingSkillAnimation => skillCoroutine != null;

    private void Awake()
    {
        // 다른 컴포넌트가 이 Driver를 호출하기 전에 필요한 참조와 모델 기준점을 준비합니다.
        Initialize();
    }

    private void LateUpdate()
    {
        if (!hasAnimatedModelAnchor) return;

        // Animator가 해당 프레임의 포즈 계산을 끝낸 뒤 모델 루트만 원래 위치와 회전으로 되돌립니다.
        // 실제 Player 이동은 CharacterController가 담당하므로 애니메이션 때문에 모델 중심이 밀리면 안 됩니다.
        // 뼈대 포즈 자체는 건드리지 않기 때문에 달리기, 점프 등의 동작은 그대로 재생됩니다.
        animatedModelRoot.localPosition = animatedModelInitialLocalPosition;
        animatedModelRoot.localRotation = animatedModelInitialLocalRotation;
    }

    /// <summary>
    /// Animator 관련 참조를 찾고 모델 루트의 최초 로컬 위치와 회전을 저장합니다.
    /// 외부 초기화 순서에서 다시 호출해도 같은 기준점을 갱신할 수 있도록 public으로 제공합니다.
    /// </summary>
    public void Initialize()
    {
        ResolveReferences();
        CacheAnimatedModelAnchor();
    }

    /// <summary>
    /// 플레이어가 바라보는 방향에 맞춰 시각 모델만 Y축으로 회전시킵니다.
    /// 실제 이동 방향과 충돌체는 PlayerController와 PlayerMovement가 별도로 관리합니다.
    /// </summary>
    public void SetFacing(bool isFacingRight)
    {
        ResolveReferences();
        if (visualRoot == null) return;

        // 오른쪽은 원래 회전, 왼쪽은 Y축 180도로 뒤집습니다.
        // 음수 Scale을 반복 적용하면 뼈대 축이나 로컬 위치가 꼬일 수 있어 회전 방식으로 통일합니다.
        visualRoot.localRotation = isFacingRight
            ? Quaternion.identity
            : Quaternion.Euler(0f, 180f, 0f);

        // 이전 설정이나 Prefab에 음수 X Scale이 남아 있더라도 항상 양수 Scale을 유지합니다.
        Vector3 localScale = visualRoot.localScale;
        localScale.x = Mathf.Abs(localScale.x);
        visualRoot.localScale = localScale;
    }

    /// <summary>
    /// 지상 이동 여부에 따라 Idle 또는 Pistol Run을 즉시 선택합니다.
    /// FSM의 IdleState와 MoveState가 자신의 애니메이션을 직접 요청하도록 만든 진입점입니다.
    /// </summary>
    public void SetLocomotion(bool isMoving)
    {
        if (!CanPlay() || skillCoroutine != null) return;

        // Animator 창의 기존 전환 조건과 디버깅 표시를 위해 파라미터도 함께 갱신합니다.
        animator.SetBool(IsMoving, isMoving);
        animator.SetBool(IsFalling, false);

        // 이동 입력은 즉각 반응해야 하므로 Exit Time을 기다리지 않고 짧게 CrossFade합니다.
        animator.CrossFadeInFixedTime(
            isMoving ? PistolRunState : IdleState,
            LocomotionBlendDuration,
            0);
    }

    /// <summary>
    /// 점프가 시작될 때 Jump State를 처음부터 재생합니다.
    /// 2단 점프에서도 같은 메서드를 다시 호출해 Jump 동작을 처음부터 보여줄 수 있습니다.
    /// </summary>
    public void PlayJump()
    {
        if (!CanPlay()) return;

        animator.SetBool(IsFalling, false);

        // 마지막 인자 0은 Jump 클립의 고정 시간 오프셋을 0초로 지정해 처음부터 시작한다는 의미입니다.
        animator.CrossFadeInFixedTime(
            JumpState,
            LocomotionBlendDuration,
            0,
            0);
    }

    /// <summary>
    /// 실제 지면 접촉 뒤 Landing 애니메이션을 처음부터 재생합니다.
    /// </summary>
    /// <returns>Landing State가 존재해 재생 요청에 성공하면 true입니다.</returns>
    public bool PlayLanding()
    {
        if (!CanPlay() || skillCoroutine != null) return false;

        if (!animator.HasState(BaseLayerIndex, LandingState))
        {
            Debug.LogWarning(
                "Player Animator에 'Base Layer.Landing' State가 없어 착지 애니메이션을 재생하지 않았습니다.",
                this);
            return false;
        }

        animator.SetBool(IsFalling, false);
        animator.CrossFadeInFixedTime(
            LandingState,
            LandingBlendDuration,
            BaseLayerIndex,
            0);
        return true;
    }

    /// <summary>
    /// Landing State에 진입한 뒤 첫 재생이 끝났는지 확인합니다.
    /// </summary>
    public bool IsLandingAnimationFinished()
    {
        if (!CanPlay() || !animator.HasState(BaseLayerIndex, LandingState))
            return true;

        AnimatorStateInfo stateInfo =
            animator.GetCurrentAnimatorStateInfo(BaseLayerIndex);

        if (stateInfo.fullPathHash != LandingState)
            return false;

        return !animator.IsInTransition(BaseLayerIndex) &&
               stateInfo.normalizedTime >= 1f;
    }

    /// <summary>
    /// 지상 대시가 시작될 때 Sliding State를 처음부터 재생합니다.
    /// 실제 대시 거리와 속도는 PlayerMovement가 담당하고 이 메서드는 화면에 보이는 연출만 담당합니다.
    /// </summary>
    public void PlaySliding()
    {
        if (!CanPlay()) return;

        animator.SetBool(IsFalling, false);

        // 대시는 입력 직후 시작되어야 하므로 Exit Time 없이 짧게 블렌딩합니다.
        animator.CrossFadeInFixedTime(
            SlidingState,
            LocomotionBlendDuration,
            0,
            0);
    }

    public bool PlayPistolShoot()
    {
        if (!CanPlay() || skillCoroutine != null) return false;
        if (animator.GetBool(IsMoving)) return false;

        if (!animator.HasState(BaseLayerIndex, PistolShootState))
        {
            Debug.LogWarning("Player Animator is missing the 'Base Layer.Pistol Shoot' state.", this);
            return false;
        }

        animator.SetTrigger(ShootTrigger);
        return true;
    }

    /// <summary>
    /// 피격 상태에 진입할 때 진행 중인 스킬 연출을 취소하고 Hit State를 재생합니다.
    /// 실제 경직과 넉백 시간은 PlayerHitState가 담당합니다.
    /// </summary>
    /// <returns>Hit State가 존재해 재생 요청에 성공하면 true입니다.</returns>
    public bool PlayHit()
    {
        if (!CanPlay()) return false;

        if (!animator.HasState(BaseLayerIndex, HitState))
        {
            Debug.LogWarning(
                "Player Animator에 'Base Layer.Hit' State가 없어 피격 애니메이션을 재생하지 않았습니다.",
                this);
            return false;
        }

        if(skillCoroutine != null)
        {
            StopCoroutine(skillCoroutine);
            skillCoroutine = null;
        }

        animator.SetBool(IsExecutingSkill, false);
        animator.SetTrigger(HitTrigger);
        return true;
    }

    /// <summary>
    /// 사망 상태에 진입할 때 진행 중인 스킬 연출을 정리하고 Death State를 재생합니다.
    /// Death State는 자동으로 Idle에 복귀하지 않으며, 부활 후 상태 전환이 직접 Idle을 재생합니다.
    /// </summary>
    /// <returns>Death State가 존재해 재생 요청에 성공하면 true입니다.</returns>
    public bool PlayDeath()
    {
        if (!CanPlay()) return false;

        if (!animator.HasState(BaseLayerIndex, DeathState))
        {
            Debug.LogWarning(
                "Player Animator에 'Base Layer.Death' State가 없어 사망 애니메이션을 재생하지 않았습니다.",
                this);
            return false;
        }

        if (skillCoroutine != null)
        {
            StopCoroutine(skillCoroutine);
            skillCoroutine = null;
        }

        animator.SetBool(IsExecutingSkill, false);
        animator.SetTrigger(DeathTrigger);
        return true;
    }

    /// <summary>
    /// Death State가 실제로 재생을 시작한 뒤 첫 재생이 끝났는지 확인합니다.
    /// 고정 대기 시간을 사용하지 않아 클립 길이나 Animator 재생 속도가 바뀌어도 완료 시점이 맞습니다.
    /// </summary>
    public bool IsDeathAnimationFinished()
    {
        // Animator 또는 Death State가 없으면 게임오버 UI가 영구히 막히지 않도록 완료로 처리합니다.
        if (!CanPlay() || !animator.HasState(BaseLayerIndex, DeathState))
            return true;

        AnimatorStateInfo stateInfo =
            animator.GetCurrentAnimatorStateInfo(BaseLayerIndex);

        // Trigger 직후에는 이전 State가 반환될 수 있으므로 Death 진입 전에는 기다립니다.
        if (stateInfo.fullPathHash != DeathState)
            return false;

        return !animator.IsInTransition(BaseLayerIndex) &&
               stateInfo.normalizedTime >= 1f;
    }

    /// <summary>
    /// 스킬 시작 애니메이션 재생 함수
    /// </summary>
    /// <param name="skillId">액티브 스킬 ID</param>
    public bool PlaySkillStart(ACTIVE_SKILL_ID skillId, float duration = 0f)
    {
        // 스킬 애니메이션이 재생 중이라면
        if (skillCoroutine != null)
            return false;

        skillStartAnimDuration = 0f;

        // 액티브 스킬 ID에 따라서
        return skillId switch
        {
            // 매그넘이라면
            ACTIVE_SKILL_ID.Magnum      => PlayMagnumSkillStart(skillId, duration),
            // 라이플(돌격소총)이라면
            ACTIVE_SKILL_ID.Rifle       => PlayRifleSkill(skillId, duration),
            // 스나이퍼(저격총)이라면
            ACTIVE_SKILL_ID.Sniper      => PlaySniperSkillStart(skillId, duration),
            // 그 외
            _                           => false
        };
    }

    /// <summary>
    /// 스킬 애니메이션 취소 함수
    /// </summary>
    /// <param name="skillId">액티브 스킬 ID</param>
    public void CancelSkill(ACTIVE_SKILL_ID skillId)
    {
        // 스킬 애니메이션이 재생 중이 아니라면
        if (skillCoroutine == null)
            return;

        // 액티브 스킬 ID에 따라서
        switch (skillId)
        {
            // 매그넘이라면
            case ACTIVE_SKILL_ID.Magnum:
                CancelMagnumSkill();
                break;
            // 라이플(돌격소총)이라면
            case ACTIVE_SKILL_ID.Rifle:
                StopRifleSkill();
                break;
            // 스나이퍼(저격총)이라면
            case ACTIVE_SKILL_ID.Sniper:
                CancelSniperSkill();
                break;
            // 그 외
            default:
                break;
        }

        // 무기 외형 착용 해제 이벤트 발행
        EventBus<ChangeWeaponState>.Publish(new((int)skillId, false));
        // 플레이어에게 스킬 실행 중 여부 이벤트 발행
        EventBus<PlayerSkillEffectExecutionChangedEvent>.Publish(new(false));
    }

    /// <summary>
    /// 스킬 종료 애니메이션 재생 함수
    /// </summary>
    /// <param name="skillId">액티브 스킬 ID</param>
    private bool PlaySkillEnd(ACTIVE_SKILL_ID skillId)
    {
        // 스킬 애니메이션이 재생 중이 아니라면
        if (skillCoroutine == null)
            return false;

        // 액티브 스킬 ID에 따라서
        return skillId switch
        {
            // 매그넘이라면
            ACTIVE_SKILL_ID.Magnum      => PlayMagnumSkillEnd(skillId),
            // 라이플(돌격소총)이라면
            ACTIVE_SKILL_ID.Rifle       => StopRifleSkill(),
            // 스나이퍼(저격총)이라면
            ACTIVE_SKILL_ID.Sniper      => PlaySniperSkillEnd(skillId),
            // 그 외
            _                           => false
        };
    }

    /// <summary>
    /// 매그넘 스킬 애니메이션의 임시 진입점입니다.
    /// MagnumSkill Trigger를 통해 Base Layer의 동명 State를 재생합니다.
    /// 스킬의 데미지, 쿨타임, 투사체 생성은 스킬 로직에서 별도로 처리해야 합니다.
    /// </summary>
    /// <returns>State가 존재해 재생 요청에 성공하면 true, 아직 준비되지 않았으면 false입니다.</returns>
    private bool PlayMagnumSkillStart(ACTIVE_SKILL_ID skillId, float duration)
    {
        if (!TryPlaySkillAnimation(MagnumSkillStartState, "MagnumSkillStartState"))
            return false;

        if (duration <= 0f)
        {
            skillStartAnimDuration = animator.GetCurrentAnimatorStateInfo(BaseLayerIndex).length;
            skillCoroutine = StartCoroutine(SkillRoutine(skillId, skillStartAnimDuration));
        }
        else
            skillCoroutine = StartCoroutine(SkillRoutine(skillId, duration));

        return true;
    }

    private bool PlayMagnumSkillEnd(ACTIVE_SKILL_ID skillId)
    {
        if (!CanPlay()) return false;

        if (!animator.HasState(BaseLayerIndex, MagnumSkillEndState))
        {
            Debug.LogWarning(
                "Player Animator에 'Base Layer.MagnumSkillEndState' State가 없어 종료 애니메이션을 재생하지 않았습니다.",
                this);
            CancelSkill(skillId);
            return false;
        }

        animator.CrossFadeInFixedTime(MagnumSkillEndState, LocomotionBlendDuration, BaseLayerIndex, 0f);
        animator.Update(0f);
        return true;
    }

    private void CancelMagnumSkill()
    {
        if (!CanPlay() || skillCoroutine == null)
            return;

        if (skillCoroutine != null)
        {
            StopCoroutine(skillCoroutine);
            skillCoroutine = null;
        }

        animator.SetBool(IsExecutingSkill, false);
        SetLocomotion(animator.GetBool(IsMoving));
    }

    /// <summary>
    /// 라이플 스킬 애니메이션의 임시 진입점입니다.
    /// RifleSkill Trigger를 통해 Base Layer의 동명 State를 재생합니다.
    /// 스킬의 연속 발사 횟수와 발사 간격은 이 Driver가 아닌 스킬 로직에서 관리합니다.
    /// </summary>
    /// <returns>State가 존재해 재생 요청에 성공하면 true, 아직 준비되지 않았으면 false입니다.</returns>
    private bool PlayRifleSkill(ACTIVE_SKILL_ID skillId, float duration)
    {
        if (!TryPlaySkillAnimation(RifleSkillState, "RifleSkill"))
            return false;

        skillCoroutine = StartCoroutine(SkillRoutine(skillId, duration));
        return true;
    }

    /// <summary>
    /// 라이플 스킬 애니메이션 종료 함수
    /// </summary>
    private bool StopRifleSkill()
    {
        // 스킬 애니메이션이 재생 중이 아니라면
        if (skillCoroutine == null)
            return false;

        // 라이플 스킬 애니메이션 종료
        animator.SetBool(IsExecutingSkill, false);
        return true;
    }

    /// <summary>
    /// 스나이퍼 스킬 애니메이션의 임시 진입점입니다.
    /// SniperSkill Trigger를 통해 Base Layer의 동명 State를 재생합니다.
    /// 조준, 관통 판정, 카메라 연출은 이 Driver가 아닌 스킬 로직에서 관리합니다.
    /// </summary>
    /// <returns>State가 존재해 재생 요청에 성공하면 true, 아직 준비되지 않았으면 false입니다.</returns>
    private bool PlaySniperSkillStart(ACTIVE_SKILL_ID skillId, float duration)
    {
        if (!TryPlaySkillAnimation(SniperSkillStartState, "SniperSkillStartState"))
            return false;

        if (duration <= 0f)
        {
            skillStartAnimDuration = animator.GetCurrentAnimatorStateInfo(BaseLayerIndex).length;
            skillCoroutine = StartCoroutine(SkillRoutine(skillId, skillStartAnimDuration));
        }
        else
            skillCoroutine = StartCoroutine(SkillRoutine(skillId, duration));

        return true;
    }

    private bool PlaySniperSkillEnd(ACTIVE_SKILL_ID skillId)
    {
        if (!CanPlay()) return false;

        if (!animator.HasState(BaseLayerIndex, SniperSkillEndState))
        {
            Debug.LogWarning(
                "Player Animator에 'Base Layer.SniperSkillEndState' State가 없어 종료 애니메이션을 재생하지 않았습니다.",
                this);
            CancelSkill(skillId);
            return false;
        }

        animator.CrossFadeInFixedTime(SniperSkillEndState, LocomotionBlendDuration, BaseLayerIndex, 0f);
        animator.Update(0f);
        return true;
    }

    private void CancelSniperSkill()
    {
        if (!CanPlay() || skillCoroutine == null)
            return;

        if (skillCoroutine != null)
        {
            StopCoroutine(skillCoroutine);
            skillCoroutine = null;
        }

        animator.SetBool(IsExecutingSkill, false);
        SetLocomotion(animator.GetBool(IsMoving));
    }

    /// <summary>
    /// 스킬 State의 존재 여부를 확인한 뒤 기존 Any State 전환 Trigger를 호출하는 공통 함수입니다.
    /// Controller에 설정된 전환 시간과 스킬 종료 후 Idle 복귀 설정을 그대로 사용합니다.
    /// </summary>
    private bool TryPlaySkillAnimation(int stateHash, string stateName)
    {
        if (!CanPlay() || skillCoroutine != null) return false;

        if (!animator.HasState(BaseLayerIndex, stateHash))
        {
            Debug.LogWarning(
                $"Player Animator에 'Base Layer.{stateName}' State가 없어 스킬 애니메이션을 재생하지 않았습니다.",
                this);
            return false;
        }

        animator.CrossFadeInFixedTime(stateHash, 0f, BaseLayerIndex, 0f);
        animator.Update(0f);
        return true;
    }

    private IEnumerator SkillRoutine(ACTIVE_SKILL_ID skillId, float duration)
    {
        // 플레이어에게 스킬 실행 중 여부 이벤트 발행
        EventBus<PlayerSkillEffectExecutionChangedEvent>.Publish(new(true));
        animator.SetBool(IsExecutingSkill, true);
        yield return new WaitForSeconds(duration);
        
        if (PlaySkillEnd(skillId))
        {
            float endDuration = animator.GetCurrentAnimatorStateInfo(BaseLayerIndex).length;
            yield return new WaitForSeconds(endDuration);
        }

        skillCoroutine = null;
        animator.SetBool(IsExecutingSkill, false);
        // 스킬 취소 이벤트 발행
        EventBus<CancelSkill>.Publish(default);
        // 플레이어에게 스킬 실행 중 여부 이벤트 발행
        EventBus<PlayerSkillEffectExecutionChangedEvent>.Publish(new(false));

        if (!Mathf.Approximately(inputReader.MoveInput.x, 0f))
            animator.SetBool(IsMoving, true);

        SetLocomotion(animator.GetBool(IsMoving));
    }

    /// <summary>
    /// 애니메이션을 재생할 수 있는 상태인지 공통 검사합니다.
    /// 각 public 메서드에서 같은 null 검사를 반복하지 않기 위한 방어 코드입니다.
    /// </summary>
    private bool CanPlay()
    {
        ResolveReferences();

        // Animator 또는 Controller가 연결되지 않은 Prefab에서도 예외가 발생하지 않도록 안전하게 중단합니다.
        if (animator == null || animator.runtimeAnimatorController == null) return false;

        // 초기화 순서상 Awake보다 먼저 호출되거나 모델이 늦게 연결된 경우 기준점을 다시 확보합니다.
        if (!hasAnimatedModelAnchor)
            CacheAnimatedModelAnchor();

        return true;
    }

    /// <summary>
    /// Inspector 참조가 비어 있을 때 Player 자식에서 Animator와 visualRoot를 자동으로 찾습니다.
    /// Prefab 연결 누락으로 애니메이션 전체가 멈추는 상황을 줄이기 위한 보조 처리입니다.
    /// </summary>
    private void ResolveReferences()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);

        // 현재 모델 구조에서는 Animator의 부모가 좌우 회전을 담당하는 visualRoot입니다.
        if (visualRoot == null && animator != null && animator.transform != transform)
            visualRoot = animator.transform.parent;

        if (inputReader == null)
            inputReader = transform.GetComponent<GameInputReader>();
    }

    /// <summary>
    /// Animator가 붙은 모델의 최초 로컬 위치와 회전을 저장합니다.
    /// 이후 LateUpdate에서 이 값을 사용해 애니메이션 Root 이동의 누적을 방지합니다.
    /// </summary>
    private void CacheAnimatedModelAnchor()
    {
        // Animator가 Player 루트 자신에게 붙어 있으면 별도의 모델 자식 Transform을 고정할 수 없습니다.
        if (animator == null || animator.transform == transform)
        {
            hasAnimatedModelAnchor = false;
            return;
        }

        animatedModelRoot = animator.transform;
        animatedModelInitialLocalPosition = animatedModelRoot.localPosition;
        animatedModelInitialLocalRotation = animatedModelRoot.localRotation;
        hasAnimatedModelAnchor = true;
    }
}
