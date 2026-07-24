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
    [Header("Attack C - Slam Impact")]
    [SerializeField] private GameObject slamImpactPrefab;
    [SerializeField] private Transform slamImpactPoint;
    [SerializeField, Min(0.1f)] private float slamImpactDuration = 0.5f;
    [Header("Attack Ultimate - Slam Impact")]
    [SerializeField] private GameObject ultimateImpactPrefab;
    [SerializeField] private Transform ultimateCenterImpactPoint;
    [SerializeField] private Transform ultimateChestImpactPoint;
    [SerializeField, Min(0.1f)] private float ultimateImpactDuration = 0.5f;

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
    private Quaternion GetImpactRotation(BossEffectCue cue, int comboIndex = 0)
    {
        if(cue == BossEffectCue.KickImpact) //패턴 A일 때 공격 회전도
        {
            if (currentFacing == Facing.Left) return Quaternion.Euler(20f, 180f, 20f);
            else return Quaternion.Euler(160f, 0f, -20f);
        }
        if(cue == BossEffectCue.SpinImpact) //패턴 B일 때 공격 회전도
        {
            return Quaternion.Euler(-10f, 0f, 0f);
        }
        //패턴C 이펙트는 각도가 필요없어서 그냥 재생해줌
        if(cue == BossEffectCue.UltimateImpact) //궁극기일 때 공격 회전도
        {
            switch (comboIndex)
            {
                case 1:
                    if (currentFacing == Facing.Left) return Quaternion.Euler(8f, 180f, 0f);
                    else return Quaternion.Euler(170f, -8f, 0f);
                case 2:
                    if (currentFacing == Facing.Left) return Quaternion.Euler(7f, 180f, 19f);
                    else return Quaternion.Euler(-22f, 50f, 9f);
                case 3:
                    if (currentFacing == Facing.Left) return Quaternion.Euler(12f, 180f, 20f);
                    else return Quaternion.Euler(-188f, 0f, -21f);
            }
        }
        return default;
    }


    //애니메이션 이벤트가 발행한 보스 VFX 요청 처리
    private void HandleBossEffect(BossEffectEvent effectEvent)
    {
        if (effectEvent.Cue == BossEffectCue.KickImpact) //패턴A 이펙트 재생
        {
            EffectManager.Instance.PlayEffect(
                kickImpactPrefab,
                kickImpactPoint.position,
                GetImpactRotation(effectEvent.Cue),
                kickImpactDuration);
        }
        if (effectEvent.Cue == BossEffectCue.SpinImpact) //패턴B 이펙트 재생
        {
            EffectManager.Instance.PlayEffect(
                spinImpactPrefab,
                spinImpactPoint.position,
                GetImpactRotation(effectEvent.Cue),
                spinImpactDuration);
        }
        if (effectEvent.Cue == BossEffectCue.SlamImpact) //패턴C 이펙트 재생
        {
            EffectManager.Instance.PlayEffect(
                slamImpactPrefab,
                slamImpactPoint.position,
                GetImpactRotation(effectEvent.Cue),
                slamImpactDuration);
        }
        if(effectEvent.Cue == BossEffectCue.UltimateImpact) //궁극기 이펙트 재생
        {
            EffectManager.Instance.PlayEffect(
                ultimateImpactPrefab,
                //궁극기 첫 공격이면 가슴 위치에서 이펙트 재생
                effectEvent.Variant == 1 ? ultimateChestImpactPoint.position : ultimateCenterImpactPoint.position,
                GetImpactRotation(effectEvent.Cue, effectEvent.Variant),
                ultimateImpactDuration);
        }
    }
}
