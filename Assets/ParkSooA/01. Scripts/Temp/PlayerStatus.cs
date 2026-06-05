using UnityEngine;

public class PlayerStatus : MonoBehaviour
{
    public void AddMaxHPValue(float value) => Debug.Log($"[Skill] 최대 체력 변경 ⇒ 변화량 : {value}");

    public void AddAttackPowerValue(float value) => Debug.Log($"[Skill] 공격력 변경 ⇒ 변화량 : {value}");

    public void AddMoveSpeedValue(float value) => Debug.Log($"[Skill] 이동 속도 변경 ⇒ 변화량 : {value}");

    public void AddAttackSpeedValue(float value) => Debug.Log($"[Skill] 공격 속도 변경 ⇒ 변화량 : {value}");
}