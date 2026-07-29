# 2026-07-28 플레이어 애니메이션 작업 내역

## 현재 대상

- 작업 씬: `InGame2`
- 플레이어 프리팹: `Assets/04_Level/Prefabs/Dev/Player/PlayerPrefabs.prefab`
- Animator Controller: `Assets/01_Art/Character/Dorothy/05_Animations/Player.controller`
- 애니메이션 제어 스크립트: `Assets/03_Code/01_Player/PlayerAnimatorDriver.cs`

## 1. 플레이어 애니메이션 책임 분리

- `PlayerController`에 있던 Animator 파라미터와 상태 재생 책임을 `PlayerAnimatorDriver`로 분리했다.
- 이동, 점프, 착지, 슬라이딩 및 모델 방향 전환은 `PlayerAnimatorDriver`가 담당한다.
- `PlayerController.Animation`을 통해 상태 스크립트에서 드라이버에 접근한다.
- 애니메이션 재생 중 모델 루트 위치와 회전이 밀리는 현상을 막기 위한 앵커 보정도 드라이버가 담당한다.

## 2. Sliding 연결 공간

- Animator에 `Sliding` 상태를 마련했다.
- `PlayerDashState` 진입 시 `PlayerAnimatorDriver.PlaySliding()`을 호출한다.
- 현재 `Sliding` 상태의 Motion에 사용할 애니메이션 클립을 연결하면 된다.

## 3. 임시 A/S/D 스킬 애니메이션

스킬 데이터별 애니메이션 설정이 아직 준비되지 않아 슬롯을 기준으로 임시 연결했다.

```text
A 슬롯 스킬 사용 성공 -> SkillA Trigger -> Skill A 상태
S 슬롯 스킬 사용 성공 -> SkillS Trigger -> Skill S 상태
D 슬롯 스킬 사용 성공 -> SkillD Trigger -> Skill D 상태
```

- Animator에 `SkillA`, `SkillS`, `SkillD` Trigger를 추가했다.
- Animator에 `Skill A`, `Skill S`, `Skill D` 상태를 추가했다.
- 세 상태의 Motion은 비어 있으므로 각각 사용할 클립을 직접 연결해야 한다.
- 각 스킬 상태는 클립 재생이 끝나면 `Idle`로 돌아간다.
- `IsMoving`이 유지 중이면 `Idle`을 거쳐 즉시 `Pistol Run`으로 복귀한다.
- 빈 슬롯이나 쿨타임 중인 스킬은 애니메이션이 재생되지 않는다.

## 실행 성공 판정

`SkillSystemModel.ExecuteActiveSkill()`과 `SkillSystemPresenter.ExecuteActiveSkill()`이 실행 성공 여부를 `bool`로 반환하도록 변경했다.

```text
슬롯에 스킬 없음 -> false
스킬 쿨타임 또는 실행 중 -> false
Ready 상태에서 실제 사용 시작 -> true
```

`SkillSystemController`는 반환값이 `true`일 때만 다음을 호출한다.

```csharp
playerAnimator.PlayTemporarySkill(slot);
```

## 나중에 교체할 부분

현재 연결은 스킬 담당자의 데이터 구조가 완성될 때까지만 사용하는 임시 구현이다.

최종적으로는 A/S/D 슬롯 자체가 아니라 `ActiveSkillData`의 애니메이션 분류값을 사용한다.

```text
현재: 슬롯 A/S/D -> 임시 애니메이션
최종: 실제 스킬 데이터 -> SkillAnimationType -> 스킬 애니메이션
확장: 무기 교체 애니메이션 -> 스킬 애니메이션 -> 공격 판정
```

교체 시 `PlayerAnimatorDriver.PlayTemporarySkill()`과 `SkillSystemController`의 임시 호출부를 제거하고, 실제 스킬 사용 성공 이벤트를 받는 브리지에서 데이터 기반 애니메이션을 재생한다.

## Unity 확인 항목

1. `Skill A`, `Skill S`, `Skill D` 상태의 Motion에 각 클립을 연결한다.
2. A/S/D 슬롯에 실제 스킬이 있을 때만 대응 애니메이션이 재생되는지 확인한다.
3. 빈 슬롯, 쿨타임, 이미 실행 중인 스킬에서는 애니메이션이 재생되지 않는지 확인한다.
4. 스킬 애니메이션 종료 후 정지 상태는 `Idle`, 이동 상태는 `Pistol Run`으로 복귀하는지 확인한다.
5. 스킬 클립 Import Settings의 Root Transform Rotation 및 Position을 Bake Into Pose로 설정했는지 확인한다.
