# 스킬 입력 수정 가이드라인

## 목적

스킬 입력은 `GameInputReader`만 Input System에서 읽고, 스킬 시스템은 Reader가 제공하는 의미 있는 입력값만 사용합니다.

```text
GameInputAction → GameInputReader → SkillSystemController → EventBus → Presenter / Model
```

## 입력 액션

`GameInputAction.inputactions`의 `Player` Action Map에서 관리합니다.

| 액션 | 키 | 슬롯 |
| --- | --- | --- |
| `SkillA` | A | `ACTIVE_SKILL_SLOT_TYPE.A` |
| `SkillS` | S | `ACTIVE_SKILL_SLOT_TYPE.S` |
| `SkillD` | D | `ACTIVE_SKILL_SLOT_TYPE.D` |

- 키 변경은 Input Actions 자산의 바인딩만 수정합니다.
- 액션은 `Player.SkillA`, `Player.SkillS`, `Player.SkillD`처럼 사용합니다.
- Action Map은 중첩되지 않으므로 `Player.Skill.SkillA` 구조는 사용하지 않습니다.

## GameInputReader 규칙

Input System API는 `GameInputReader`에서만 호출합니다.

```csharp
#region Skill Input
public bool SkillAPressed => _input.Player.SkillA.WasPressedThisFrame();
public bool SkillAReleased => _input.Player.SkillA.WasReleasedThisFrame();

public bool SkillSPressed => _input.Player.SkillS.WasPressedThisFrame();
public bool SkillSReleased => _input.Player.SkillS.WasReleasedThisFrame();

public bool SkillDPressed => _input.Player.SkillD.WasPressedThisFrame();
public bool SkillDReleased => _input.Player.SkillD.WasReleasedThisFrame();
#endregion
```

| 필요한 시점 | Input System 메서드 |
| --- | --- |
| 누른 프레임 | `WasPressedThisFrame()` |
| 뗀 프레임 | `WasReleasedThisFrame()` |
| 누르고 있는 동안 | `IsPressed()` |

`Input.GetKeyDown`, `Input.GetKeyUp`, `Input.GetKey`를 스킬 코드에서 직접 사용하지 않습니다.

## SkillSystemController 규칙

`SkillSystemController`는 `GameInputReader`를 참조하고, Reader의 입력을 기존 스킬 슬롯 이벤트로 변환합니다.

```csharp
if (_inputReader.SkillAPressed)
{
    EventBus<StartedPressSkillSlot>.Publish(
        new StartedPressSkillSlot(ACTIVE_SKILL_SLOT_TYPE.A));
}

if (_inputReader.SkillAReleased)
{
    EventBus<CanceledPressSkillSlot>.Publish(
        new CanceledPressSkillSlot(ACTIVE_SKILL_SLOT_TYPE.A));
}
```

`SkillS`와 `SkillD`도 각각 S, D 슬롯으로 같은 방식으로 변환합니다.

이벤트에 슬롯 값이 이미 포함되므로 실행과 취소 메서드에서 다시 A/S/D를 `switch`로 구분하지 않습니다.

```csharp
private void ExecuteSkill(StartedPressSkillSlot input)
{
    if (presenter == null)
        return;

    presenter.ExecuteActiveSkill(input.type);
}

private void CancelSkill(CanceledPressSkillSlot input)
{
    if (presenter == null)
        return;

    presenter.CancelActiveSkill(input.type);
}
```

## 책임 분리

- `GameInputAction`: 액션 이름과 키 바인딩
- `GameInputReader`: Input System 값을 읽어 입력 상태 제공
- `SkillSystemController`: 입력 상태를 스킬 슬롯 이벤트로 변환
- `SkillSystemPresenter`와 `SkillSystemModel`: 장착 스킬 확인, 실행, 취소, 쿨타임 처리

이번 구조에서는 슬롯 enum `ACTIVE_SKILL_SLOT_TYPE.A/S/D`와 스킬 데이터는 유지합니다.

## 확인 항목

- A/S/D를 누르면 SkillA/SkillS/SkillD의 Pressed 값이 해당 프레임에만 true가 된다.
- A/S/D를 떼면 Released 값이 해당 프레임에만 true가 된다.
- `SkillSystemController`에 Legacy `Input.GetKey*` 호출이 남아 있지 않다.
- 기존 A/S/D 슬롯에 장착된 스킬이 정상적으로 시작·취소된다.
