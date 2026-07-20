# GameInputReader 사용 가이드

## 원칙

- 입력은 `Input.GetKeyDown`으로 직접 읽지 않는다.
- `GameInputReader`의 속성을 통해서만 읽는다.
- 키 변경은 `GameInputAction.inputactions`에서만 한다.

## UI 입력

| 용도 | 속성 |
| --- | --- |
| 메뉴 열기/닫기 | `MenuPressed` |
| 타이틀 시작 | `TitleStartPressed` |
| Pause 이전/다음 탭 | `PreviousPauseTabPressed` / `NextPauseTabPressed` |
| 확인/계속 | `SubmitPressed` |
| 방향 이동 | `UINavigationInput` (`Vector2`) |

```csharp
if (inputReader.SubmitPressed)
{
    Confirm();
}
```

## 스킬 입력

| 키 | 속성 |
| --- | --- |
| A | `SkillAPressed`, `SkillAReleased` |
| S | `SkillSPressed`, `SkillSReleased` |
| D | `SkillDPressed`, `SkillDReleased` |

```csharp
if (inputReader.SkillAPressed)
{
    UseSkillA();
}
```

## 테스트 입력

F1~F10은 `TestF1Pressed`~`TestF10Pressed`로 사용한다.
테스트 목적이 끝난 코드는 함께 제거한다.

## 참조 연결

입력이 필요한 컴포넌트는 `GameInputReader`를 Inspector에서 참조로 연결한다.
`static`, `Find`, 새 `GameInputReader` 생성은 사용하지 않는다.
