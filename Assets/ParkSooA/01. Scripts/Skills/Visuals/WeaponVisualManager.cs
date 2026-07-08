using System.Collections.Generic;
using UnityEngine;

public class WeaponVisualManager : MonoBehaviour
{
    [Header("무기 컨테이너")]
    [Tooltip("무기 모와두는 곳")]
    [SerializeField] private Transform weaponContainer;                 // 무기 컨테이너

    private readonly Dictionary<int, GameObject> weapons = new();       // 모든 무기 딕셔너리

    private void OnEnable()
    {
        // 액티브 스킬 장착 이벤트 구독
        EventBus<WeaponVisualAddData>.action += AddWeapon;
        // 무기 외형 상태 변경 이벤트 구독
        EventBus<ChangeWeaponState>.action += ChangeWeapon;
    }

    private void OnDisable()
    {
        // 액티브 스킬 장착 이벤트 구독 해제
        EventBus<WeaponVisualAddData>.action -= AddWeapon;
        // 무기 외형 상태 변경 이벤트 구독 해제
        EventBus<ChangeWeaponState>.action -= ChangeWeapon;
    }

    /// <summary>
    /// 무기 외형 생성 및 추가 함수
    /// </summary>
    private void AddWeapon(WeaponVisualAddData skill)
    {
        // ID에 해당하는 무기가 없다면
        if(!weapons.ContainsKey(skill.id))
        {
            // 무기 프리팹이 있다면
            if(skill.weapon != null)
            {
                // 무기 오브젝트 생성 후 딕셔너리에 저장
                weapons[skill.id] = Instantiate(skill.weapon, weaponContainer);
                // 무기 비활성화
                weapons[skill.id].SetActive(false);
                Debug.Log($"[Weapon] 무기 외형 저장 완료 => 입력 - 스킬 ID : {skill.id}", weaponContainer);
            }
        }
    }

    /// <summary>
    /// 무기 외형 변경 함수
    /// </summary>
    private void ChangeWeapon(ChangeWeaponState change)
    {
        // 변경할 무기가 없다면
        if (!weapons.TryGetValue(change.id, out var weaponVisual))
            return;

        // 무기 외형 상태 변경
        weaponVisual.SetActive(change.isActiveWeapon);

        // 활성화 상태이고 무기 외형에 무기 인터페이스가 있다면
        if(change.isActiveWeapon && weaponVisual.TryGetComponent<IWeapon>(out var weapon))
            // 액티브 스킬 실행 위치들 변경 이벤트 발행
            EventBus<ChangeActiveSkillExecutePositions>
                .Publish(new ChangeActiveSkillExecutePositions(weapon.FirePoints));
    }
}