using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerStatus))]
[RequireComponent(typeof(PlayerLifeTracker))]
[RequireComponent(typeof(PlayerHealItemInventory))]
public class CheckpointHealSystem : MonoBehaviour
{
    [Header("Player References")]
    [SerializeField] private PlayerStatus playerStatus;
    [SerializeField] private PlayerLifeTracker lifeTracker;
    [SerializeField]
    private PlayerHealItemInventory healItemInventory;

    // 같은 Player 오브젝트의 회복 관련 컴포넌트를 준비합니다
    private void Awake()
    {
        playerStatus ??= GetComponent<PlayerStatus>();
        lifeTracker ??= GetComponent<PlayerLifeTracker>();
        healItemInventory ??=
            GetComponent<PlayerHealItemInventory>();
    }

    // Definition 설정에 따라 부족한 자원만 최대치까지 복구합니다
    public bool RecoverMissingResources(
        CheckpointDefinition definition)
    {
        if (definition == null)
        {
            return false;
        }

        bool didRecover = false;

        if (definition.RestoreHealth)
        {
            didRecover |= RestoreHealthToFull();
        }

        if (definition.RestoreLives)
        {
            didRecover |= RestoreLivesToFull();
        }

        if (definition.RestoreHealItems)
        {
            didRecover |= RestoreHealItemsToFull();
        }

        return didRecover;
    }

    // 현재 Player 상태를 체크포인트 스냅샷으로 반환합니다
    public void CaptureSnapshot(
        out float currentHP,
        out int currentHealItemCount)
    {
        currentHP =
            playerStatus != null
                ? playerStatus.GetCurrentHP()
                : 0f;

        currentHealItemCount =
            healItemInventory != null
                ? healItemInventory.CurrentCount
                : 0;
    }

    // 체력이 부족할 때 최대 체력까지 회복합니다
    private bool RestoreHealthToFull()
    {
        if (playerStatus == null)
        {
            return false;
        }

        float missingHP =
            playerStatus.GetMaxHP() -
            playerStatus.GetCurrentHP();

        if (missingHP <= 0f)
        {
            return false;
        }

        playerStatus.Heal(missingHP);
        return true;
    }

    // 잔기가 부족할 때 시작 잔기 수까지 복구합니다
    private bool RestoreLivesToFull()
    {
        if (lifeTracker == null)
        {
            return false;
        }

        return lifeTracker.RestoreToFull();
    }

    // 회복 아이템이 부족할 때 최대 보유량까지 복구합니다
    private bool RestoreHealItemsToFull()
    {
        if (healItemInventory == null ||
            healItemInventory.CurrentCount >=
            healItemInventory.MaxCount)
        {
            return false;
        }

        healItemInventory.RestoreCount(
            healItemInventory.MaxCount);
        return true;
    }
}
