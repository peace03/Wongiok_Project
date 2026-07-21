using UnityEngine;

//신데렐라 공격별 VFX 재생 담당
public class CinderellaAttackEffectController : MonoBehaviour
{
    [Header("Attack A - Kick Impact")]
    [SerializeField] private GameObject kickImpactPrefab;
    [SerializeField] private Transform kickImpactPoint;
    [SerializeField, Min(0.1f)] private float kickImpactDuration = 0.2f;

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
    private Quaternion GetKickImpactRotation()
    {
        if (currentFacing == Facing.Left) return Quaternion.Euler(20f, 180f, 20f);
        return Quaternion.Euler(160f, 0f, -20f);
    }


    //애니메이션 이벤트가 발행한 보스 VFX 요청 처리
    private void HandleBossEffect(BossEffectEvent effectEvent)
    {
        if (effectEvent.AttackId == BossAttackIds.A)
        {
            EffectManager.Instance.PlayEffect(
                kickImpactPrefab,
                kickImpactPoint.position,
                GetKickImpactRotation(),
                kickImpactDuration);
        }
    }
}
