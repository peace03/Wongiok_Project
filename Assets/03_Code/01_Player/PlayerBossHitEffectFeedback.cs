using UnityEngine;

// 보스에게 실제 피해를 받았을 때만 플레이어 피격 이펙트를 재생한다.
public class PlayerBossHitEffectFeedback : MonoBehaviour
{
    [Header("Boss Hit Effect")]
    [SerializeField] private GameObject bossHitEffectPrefab;
    [SerializeField] private Transform effectSpawnPoint;
    [SerializeField, Min(0.01f)] private float effectDuration = 0.5f;

    // 플레이어가 활성화되면 실제 피해 이벤트 수신을 시작한다.
    private void OnEnable()
    {
        EventBus<PlayerDamagedEvent>.action += HandlePlayerDamaged;
    }

    // 플레이어가 비활성화되면 이벤트 구독을 해제해 중복 콜백을 방지한다.
    private void OnDisable()
    {
        EventBus<PlayerDamagedEvent>.action -= HandlePlayerDamaged;
    }

    // 보스가 적용한 실제 피해인 경우에만 지정된 위치에서 이펙트를 재생한다.
    private void HandlePlayerDamaged(PlayerDamagedEvent eventData)
    {
        // 다른 플레이어의 이벤트에는 반응하지 않는다.
        if (eventData.PlayerObject != gameObject)
            return;

        // 패링 등으로 무시된 피해는 이벤트가 발행되지 않고, 일반 피해도 이펙트 대상이 아니다.
        if (eventData.Source != PlayerDamageSource.Boss)
            return;

        // 인스펙터 설정 또는 이펙트 시스템 준비 전에는 안전하게 재생을 건너뛴다.
        if (bossHitEffectPrefab == null || effectSpawnPoint == null || EffectManager.Instance == null)
            return;

        // 풀링 이펙트가 설정 시간 뒤 반환되도록 지속 시간을 함께 전달한다.
        EffectManager.Instance.PlayEffect(
            bossHitEffectPrefab,
            effectSpawnPoint.position,
            effectSpawnPoint.rotation,
            effectDuration);
    }
}
