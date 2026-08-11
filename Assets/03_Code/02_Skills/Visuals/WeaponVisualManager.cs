using UnityEngine;
using System.Collections.Generic;

public class WeaponVisualManager : MonoBehaviour, IInitializable
{
    [Header("무기 소유자")]
    [Tooltip("플레이어, 몬스터, NPC 등등")]
    [SerializeField] private Transform owner;                           // 소유자

    private Quaternion magnumRotation;                                  // 매그넘에 추가할 각도

    private readonly Dictionary<int, GameObject> weapons = new();       // 모든 무기 딕셔너리

    public int Priority => (int)InitOrder.Skill;                        // 중요도

    private void OnDisable()
    {
        // 액티브 스킬 장착 이벤트 구독 해제
        EventBus<WeaponVisualAddData>.action -= AddWeapon;
        // 무기 외형 상태 변경 이벤트 구독 해제
        EventBus<ChangeWeaponState>.action -= ChangeWeapon;
    }

    /// <summary>
    /// 초기화 함수
    /// </summary>
    public void Init()
    {
        // 매그넘에 추가할 각도 받아오기
        magnumRotation = transform.rotation;

        // 소유자가 있고 따라다니는 대상이 소유자가 아니라면
        if (owner != null && transform.parent != owner)
        {
            // 위치와 각도를 소유자로 설정
            transform.SetPositionAndRotation(owner.position, owner.rotation);
            // 따라다니는 대상을 소유자로 설정
            transform.SetParent(owner, true);
        }

        // 액티브 스킬 장착 이벤트 구독
        EventBus<WeaponVisualAddData>.action += AddWeapon;
        // 무기 외형 상태 변경 이벤트 구독
        EventBus<ChangeWeaponState>.action += ChangeWeapon;
    }

    /// <summary>
    /// 무기 외형 생성 및 추가 함수
    /// </summary>
    private void AddWeapon(WeaponVisualAddData skill)
    {
        // ID에 해당하는 무기가 있다면
        if (weapons.ContainsKey(skill.id))
            return;
        // 무기 프리팹이 없다면
        else if (skill.weapon == null)
            return;

        // 무기 오브젝트 생성 후 딕셔너리에 저장
        weapons[skill.id] = Instantiate(skill.weapon, transform);
        // 무기 비활성화
        weapons[skill.id].SetActive(false);
    }

    /// <summary>
    /// 무기 외형 변경 함수
    /// </summary>
    private void ChangeWeapon(ChangeWeaponState change)
    {
        // 변경할 무기가 없다면
        if (!weapons.TryGetValue(change.id, out var weaponVisual))
            return;

        transform.localRotation = Quaternion.identity;
        // 무기 외형 상태 변경
        weaponVisual.SetActive(change.isActiveWeapon);

        // 총구 소유 인터페이스가 없다면
        if (!weaponVisual.TryGetComponent<IHaveFirePoint>(out var weapon))
            return;

        // 무기가 활성화 상태라면
        if (change.isActiveWeapon)
        {
            // 매그넘 무기라면
            if (change.id == (int)ACTIVE_SKILL_ID.Magnum)
                // 각도 변환
                transform.localRotation = magnumRotation;

            // 액티브 스킬 실행 위치들 변경 이벤트 발행
            EventBus<ChangeActiveSkillExecutePositions>.Publish(new(weapon.FirePoints));
            // 무기 애니메이션 재생
            weapon.PlayAnimation();
        }
    }
}