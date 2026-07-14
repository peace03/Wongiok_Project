using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [시스템 아키텍처: 이벤트 주도적 다형성 충돌체 관리자]
/// 애니메이터나 상태 머신(FSM)과 직접 결합하지 않고, EventBus를 통해 시각/논리적 변화(공격, 방향 전환, 패링)를 수신하여 
/// 물리적 판정(BoxCollider)을 제어하는 단일 책임(SRP) 클래스입니다.
/// </summary>
public class AttackColliders_Y : MonoBehaviour
{
    // [설계 의도]: 기존 인덱스(List) 기반의 하드코딩된 한계를 극복하기 위해 도입된 데이터 래퍼 클래스.
    // 문자열 ID를 사용하여 공격 타입이 추가/삭제되어도 인덱스가 밀리는 버그를 원천 차단합니다.
    [Serializable]
    private class AttackColliderBinding
    {
        public string attackId = string.Empty;
        public BoxCollider attackCollider = null;
        public Vector3 defaultPos = Vector3.zero; // 좌표 오염 방지용 불변(Immutable) 원본 좌표
    }

    #region [레거시 및 신규 데이터 인스펙터]
    // [하위 호환성 유지]: 과거 인덱스 매칭 방식을 사용하던 프리팹을 위한 레거시 리스트
    [SerializeField] private List<BoxCollider> attackColliders;
    [SerializeField] private List<Vector3> defaultPos;

    // [확장성 확보]: 신규 추가되는 공격 패턴들을 위한 명시적 바인딩 리스트 (Inspector 노출용)
    [SerializeField] private List<AttackColliderBinding> attackColliderBindings = new List<AttackColliderBinding>();
    #endregion

    #region [메모리 최적화 캐싱]
    // [성능 최적화]: 전투 중 List 순회(O(N))를 피하기 위해, 시작 시점에 Dictionary(O(1))로 메모리에 올려 빠른 검색을 보장합니다.
    private readonly Dictionary<string, AttackColliderBinding> bindingByAttackId = new Dictionary<string, AttackColliderBinding>();

    // 현재 보스 프리팹이 신규(명시적 바인딩) 방식을 사용하는지, 레거시(리스트) 방식을 사용하는지 판별하는 스위치
    private bool useExplicitBindings;
    #endregion

    private void Awake()
    {
        RebuildBindings();
    }

    private void OnEnable()
    {
        RebuildBindings();

        // [디커플링]: 보스 로직(Cinderella_Patterns 등)을 몰라도, 전역 이벤트만 듣고 스스로 작동합니다.
        EventBus<ColliderToggleEvent>.action += ToggleCollider;
        EventBus<BossFacingChangeEvent>.action += ChangeColliderPos;
        EventBus<ParryKeyDown>.action += OffCollider;
    }

    private void OnDisable()
    {
        EventBus<ColliderToggleEvent>.action -= ToggleCollider;
        EventBus<BossFacingChangeEvent>.action -= ChangeColliderPos;
        EventBus<ParryKeyDown>.action -= OffCollider;
    }

    /// <summary>
    /// 공격 애니메이션의 특정 프레임(Edge Trigger)에서 호출되어 콜라이더를 켜고 끕니다.
    /// </summary>
    public void ToggleCollider(ColliderToggleEvent data)
    {
        if (!data.state)
        {
            DisableAllColliders();
            return;
        }

        // Dictionary를 통한 O(1) 고속 검색으로 프레임 드랍 방지
        if (TryGetCollider(data.attackId, out BoxCollider targetCollider))
            targetCollider.enabled = true;
    }

    /// <summary>
    /// 플레이어가 패링에 성공했을 때, 진행 중이던 모든 공격 판정을 즉시 회수(비활성화)합니다.
    /// </summary>
    public void OffCollider(ParryKeyDown data)
    {
        DisableAllColliders();
    }

    /// <summary>
    /// 보스의 시선이 바뀔 때 콜라이더의 로컬 위치를 대칭 이동시킵니다.
    /// </summary>
    public void ChangeColliderPos(BossFacingChangeEvent data)
    {
        if (useExplicitBindings)
        {
            foreach (AttackColliderBinding binding in attackColliderBindings)
            {
                if (binding?.attackCollider == null) continue;
                // [안전성]: 현재 좌표를 곱해서 뒤집지 않고, 항상 '원본 좌표(defaultPos)'를 기준으로 뒤집습니다.
                // 이는 여러 번 연속으로 방향을 전환할 때 발생하는 부동소수점 오차(Drift)나 좌표 오염을 막습니다.
                binding.attackCollider.center = GetMirroredCenter(binding.defaultPos, data.dir);
            }
            return;
        }

        // 레거시(리스트) 방식 처리
        if (attackColliders == null) return;
        for (int i = 0; i < attackColliders.Count; i++)
        {
            BoxCollider attackCollider = attackColliders[i];
            if (attackCollider == null) continue;

            Vector3 baseCenter = GetFallbackDefaultPos(i, attackCollider);
            attackCollider.center = GetMirroredCenter(baseCenter, data.dir);
        }
    }

    /// <summary>
    /// [초기화]: Inspector에 등록된 List 데이터를 읽어 고속 검색용 Dictionary로 마이그레이션(Migration)합니다.
    /// </summary>
    private void RebuildBindings()
    {
        bindingByAttackId.Clear();
        useExplicitBindings = false;
        if (attackColliderBindings == null) return;

        foreach (AttackColliderBinding binding in attackColliderBindings)
        {
            if (binding == null || binding.attackCollider == null || string.IsNullOrEmpty(binding.attackId)) continue;

            // 중복 키 방지 및 캐싱
            if (!bindingByAttackId.ContainsKey(binding.attackId))
            {
                bindingByAttackId.Add(binding.attackId, binding);
                useExplicitBindings = true; // 유효한 명시적 바인딩이 단 하나라도 있으면 신규 모드로 작동
            }
        }
    }

    /// <summary>
    /// 신규 모드(Dictionary)와 레거시 모드(Index)를 분기하여 안전하게 콜라이더를 반환합니다.
    /// </summary>
    private bool TryGetCollider(string attackId, out BoxCollider targetCollider)
    {
        targetCollider = null;

        if (useExplicitBindings)
        {
            // O(1) 조회
            if (!string.IsNullOrEmpty(attackId) && bindingByAttackId.TryGetValue(attackId, out AttackColliderBinding binding))
                targetCollider = binding.attackCollider;
            return targetCollider != null;
        }

        // 레거시: Enum 형태의 문자열을 정수 인덱스로 변환하여 List에서 조회 (결합도가 높았던 과거 방식)
        int index = BossAttackIds.ToDefaultIndex(attackId);
        if (index < 0 || attackColliders == null || index >= attackColliders.Count) return false;

        targetCollider = attackColliders[index];
        return targetCollider != null;
    }

    private void DisableAllColliders()
    {
        // 신규/레거시 분기에 따른 순회 비활성화
        if (useExplicitBindings)
        {
            foreach (AttackColliderBinding binding in attackColliderBindings)
            {
                if (binding?.attackCollider != null)
                    binding.attackCollider.enabled = false;
            }
            return;
        }

        if (attackColliders == null) return;
        foreach (BoxCollider attackCollider in attackColliders)
        {
            if (attackCollider != null)
                attackCollider.enabled = false;
        }
    }

    /// <summary>
    /// 레거시 방식에서 안전한 원본 좌표(Default Center)를 추출합니다.
    /// </summary>
    private Vector3 GetFallbackDefaultPos(int index, BoxCollider attackCollider)
    {
        if (defaultPos != null && index >= 0 && index < defaultPos.Count)
            return defaultPos[index];

        return attackCollider.center; // fallback 좌표가 없으면 현재 좌표 반환 (위험 감수)
    }

    /// <summary>
    /// 방향(Facing)에 따라 X축을 수학적으로 반전시킵니다.
    /// </summary>
    private Vector3 GetMirroredCenter(Vector3 baseCenter, Facing dir)
    {
        if (dir == Facing.Left) return baseCenter;
        return new Vector3(-baseCenter.x, baseCenter.y, baseCenter.z); // 오른쪽을 볼 때 로컬 좌표 반전
    }
}