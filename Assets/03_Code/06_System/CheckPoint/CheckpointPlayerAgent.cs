using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class CheckpointPlayerAgent : MonoBehaviour
{
    private readonly List<CheckpointInteractionZone> zones =
        new List<CheckpointInteractionZone>();

    private PlayerStatus playerStatus;

    // 플레이어 상태 참조를 준비합니다
    private void Awake()
    {
        playerStatus = GetComponent<PlayerStatus>();
    }

    // 방향키 위 입력으로 가장 가까운 체크포인트를 활성화합니다
    private void Update()
    {
        if (!CanProcessInteraction())
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;

        if (keyboard == null ||
            !keyboard.upArrowKey.wasPressedThisFrame)
        {
            return;
        }

        CheckpointInteractionZone zone =
            GetNearestAvailableZone();

        zone?.TryActivate(this);
    }

    // 플레이어가 진입한 체크포인트 영역을 등록합니다
    public void RegisterZone(CheckpointInteractionZone zone)
    {
        if (zone == null || zones.Contains(zone))
        {
            return;
        }

        zones.Add(zone);
    }

    // 플레이어가 벗어난 체크포인트 영역을 제거합니다
    public void UnregisterZone(CheckpointInteractionZone zone)
    {
        if (zone == null)
        {
            return;
        }

        zones.Remove(zone);
    }

    // 현재 입력과 플레이어 생존 상태가 상호작용을 허용하는지 확인합니다
    private bool CanProcessInteraction()
    {
        CleanupInvalidZones();

        if (zones.Count == 0)
        {
            return false;
        }

        if (Time.timeScale <= 0f)
        {
            return false;
        }

        if (playerStatus == null)
        {
            playerStatus = GetComponent<PlayerStatus>();
        }

        return playerStatus == null ||
               playerStatus.GetCurrentHP() > 0f;
    }

    // 등록된 영역 중 현재 활성화 가능한 가장 가까운 영역을 찾습니다
    private CheckpointInteractionZone GetNearestAvailableZone()
    {
        CheckpointInteractionZone nearestZone = null;
        float nearestSqrDistance = float.MaxValue;

        for (int i = 0; i < zones.Count; i++)
        {
            CheckpointInteractionZone candidate = zones[i];

            if (candidate == null ||
                !candidate.CanActivate(this))
            {
                continue;
            }

            float sqrDistance =
                (candidate.transform.position - transform.position)
                .sqrMagnitude;

            if (sqrDistance >= nearestSqrDistance)
            {
                continue;
            }

            nearestSqrDistance = sqrDistance;
            nearestZone = candidate;
        }

        return nearestZone;
    }

    // 파괴되었거나 비활성화된 체크포인트 영역을 목록에서 제거합니다
    private void CleanupInvalidZones()
    {
        for (int i = zones.Count - 1; i >= 0; i--)
        {
            CheckpointInteractionZone zone = zones[i];

            if (zone == null || !zone.isActiveAndEnabled)
            {
                zones.RemoveAt(i);
            }
        }
    }
}
