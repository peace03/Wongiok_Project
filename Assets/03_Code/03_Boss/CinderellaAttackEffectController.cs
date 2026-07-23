using UnityEngine;

//신데렐라 공격별 VFX 재생 담당
public class CinderellaAttackEffectController : MonoBehaviour
{
    [Header("Attack A - Kick Impact")]
    [SerializeField] private GameObject kickImpactPrefab;
    [SerializeField] private Transform kickImpactPoint;
    [SerializeField, Min(0.1f)] private float kickImpactDuration = 0.5f;
    [Header("Attack B - Spin Impact")]
    [SerializeField] private GameObject spinImpactPrefab;
    [SerializeField] private Transform spinImpactPoint;
    [SerializeField, Min(0.1f)] private float spinImpactDuration = 0.5f;

    private Facing currentFacing = Facing.Left;

    private void OnEnable()
    {
        EventBus<BossFacingChangeEvent>.action += HandleFacingChanged;
        EventBus<BossEffectEvent>.action += HandleBossEffect;
    }
    private void OnDisable()
    {
        EventBus<BossFacingChangeEvent>.action -= HandleFacingChanged;
        EventBus<BossEffectEvent>.action -= HandleBossEffect;
    }

    //보스 정면 방향 변화 이벤트 구독
    private void HandleFacingChanged(BossFacingChangeEvent data) { currentFacing = data.dir; }

    //방향값 설정
    private Quaternion GetImpactRotation(BossEffectCue cue)
    {
        if(cue == BossEffectCue.KickImpact) //패턴 A일 때 공격 회전도
        {
            if (currentFacing == Facing.Left) return Quaternion.Euler(20f, 180f, 20f);
            else return Quaternion.Euler(160f, 0f, -20f);
        }
        if(cue == BossEffectCue.SpinImpact)
        {
            return Quaternion.Euler(-10f, 0f, 0f);
        }
        return default;
    }


    //애니메이션 이벤트가 발행한 보스 VFX 요청 처리
    private void HandleBossEffect(BossEffectEvent effectEvent)
    {
        if (effectEvent.Cue == BossEffectCue.KickImpact)
        {
            EffectManager.Instance.PlayEffect(
                kickImpactPrefab,
                kickImpactPoint.position,
                GetImpactRotation(effectEvent.Cue),
                kickImpactDuration);
        }
        if (effectEvent.Cue == BossEffectCue.SpinImpact)
        {
            Debug.Log("SpinImpact");
            EffectManager.Instance.PlayEffect(
                spinImpactPrefab,
                spinImpactPoint.position,
                GetImpactRotation(effectEvent.Cue),
                spinImpactDuration);
        }
    }
}
