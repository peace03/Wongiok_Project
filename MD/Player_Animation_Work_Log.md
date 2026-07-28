# Player Animation Work Log

작성일: 2026-07-26

## 작업 대상

- 작업 씬: `Assets/04_Level/Scenes/InGame2.unity`
- 플레이어 프리팹: `Assets/04_Level/Prefabs/Dev/Player/PlayerPrefabs.prefab`
- Animator Controller: `Assets/01_Art/Character/Dorothy/05_Animations/Dorothy_AniController.controller`
- 게임용 애니메이션: `Assets/01_Art/Dev/Player/Animation`

## 최종 Animator 흐름

### 이동과 정지

```text
Pistol Idle
  -> IsMoving = true
Fast Run
  -> IsMoving = false
Stop
  -> 클립 종료
Pistol Idle
```

- `Stop`은 `Fast Run`이 끝날 때만 재생한다.
- `Stop` 재생 중 `IsMoving = true`가 되면 즉시 `Fast Run`으로 복귀한다.
- `Stop`은 기본 상태나 공용 전환으로 사용하지 않는다.

### 피격

```text
Any State
  -> Hit Trigger
Hit
  -> IsMoving = true  : Fast Run
  -> IsMoving = false : Pistol Idle
```

- `Hit.anim` 원본 길이는 1초이다.
- 게임의 피격 경직 시간은 `PlayerStatus.HitStunDuration = 0.2f`이다.
- Animator의 `Hit` 상태 속도를 5배로 설정해 약 0.2초에 맞췄다.
- `PlayerHitState.EnterState()`에서 `PlayerAttack.PlayHitAnimation()`을 호출한다.
- 사망 판정은 피격 상태 진입보다 먼저 처리되므로 사망 시 Hit Trigger를 호출하지 않는다.

## 방향 전환 수정

기존에는 `visualRoot.localScale.x`를 양수/음수로 바꿔 모델을 Flip했다.
Humanoid 모델에 음수 Scale을 적용하면 본 좌표계가 반전되어 애니메이션 재생 중 포즈가 튈 수 있다.

현재는 다음 방식으로 변경했다.

- 오른쪽: `visualRoot.localRotation = Quaternion.identity`
- 왼쪽: `visualRoot.localRotation = Quaternion.Euler(0f, 180f, 0f)`
- `visualRoot.localScale.x`는 항상 양수로 유지
- 이전에 시험한 초당 1080도 회전 보간은 제거

## Root Transform 버그 수정

### 증상

- 애니메이션을 재생할 때 모델이 기준축에서 조금씩 앞으로 이동했다.
- 기준 Transform은 제자리에 있지만 모델이 축에서 벗어났다.
- 벗어난 모델을 기준축 중심으로 방향 전환하면서 순간이동처럼 보였다.
- 특히 `Stop` 종료 후 축 회전과 전진 오프셋이 눈에 띄었다.

### 원인

클립에 `RootT`와 `RootQ` 커브가 포함되어 있었고, 일부 클립의 실제 `Bake Into Pose` 설정이 꺼져 있었다.

Unity 직렬화 필드의 역할은 다음과 같다.

| Inspector 설정 | 직렬화 필드 |
| --- | --- |
| Root Transform Rotation / Bake Into Pose | `m_LoopBlendOrientation` |
| Root Transform Position (Y) / Bake Into Pose | `m_LoopBlendPositionY` |
| Root Transform Position (XZ) / Bake Into Pose | `m_LoopBlendPositionXZ` |
| Based Upon: Original | `m_KeepOriginalOrientation`, `m_KeepOriginalPositionY`, `m_KeepOriginalPositionXZ` |

`m_KeepOriginal...` 값만 변경하는 것은 Bake Into Pose를 켜는 작업이 아니다.

### 적용 상태

다음 클립은 Rotation, Position Y, Position XZ의 Bake Into Pose와 Original 기준을 모두 활성화했다.

- `Pistol Idle.anim`
- `Fast Run.anim`
- `Stop.anim`
- `Hit.anim`

플레이어의 실제 이동은 `CharacterController`와 플레이어 이동 코드가 담당하며, 애니메이션 루트 이동은 사용하지 않는다.

## 수정 파일

- `Assets/01_Art/Character/Dorothy/05_Animations/Dorothy_AniController.controller`
- `Assets/01_Art/Dev/Player/Animation/Pistol Idle.anim`
- `Assets/01_Art/Dev/Player/Animation/Fast Run.anim`
- `Assets/01_Art/Dev/Player/Animation/Stop.anim`
- `Assets/01_Art/Dev/Player/Animation/Hit.anim`
- `Assets/03_Code/01_Player/PlayerController.cs`
- `Assets/03_Code/01_Player/PlayerAttack.cs`
- `Assets/03_Code/01_Player/State/PlayerHitState.cs`

## 검증 결과

- `dotnet build .\Assembly-CSharp.csproj --no-restore`
- 컴파일 오류: 0개
- 기존 컴파일 경고: 30개
- 경고는 이번 애니메이션 작업과 무관한 기존 직렬화 필드 및 미사용 필드 경고이다.

## Unity 수동 확인

1. `InGame2`에서 `Pistol Idle -> Fast Run -> Stop -> Pistol Idle` 순서를 확인한다.
2. Stop 중 이동 입력 시 즉시 Fast Run으로 복귀하는지 확인한다.
3. 좌우 방향 전환 시 모델이 기준축에서 순간이동하지 않는지 확인한다.
4. 반복 이동과 정지 후 모델이 기준축에서 앞으로 누적 이동하지 않는지 확인한다.
5. Idle, Fast Run, Stop 상태에서 각각 피격해 Hit이 즉시 재생되는지 확인한다.
6. Hit 종료 후 이동 입력에 따라 Fast Run 또는 Pistol Idle로 복귀하는지 확인한다.
7. Hit 재생 전후 모델의 로컬 위치와 회전축이 유지되는지 확인한다.

## 이번 범위에서 제외

- Pistol 공격 애니메이션과 상체 레이어
- Die 애니메이션
- Jump, Dash, Heal, Parry 애니메이션
- Animation Event 기반 공격 발사 시점
