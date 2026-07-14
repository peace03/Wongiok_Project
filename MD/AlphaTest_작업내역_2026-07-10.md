# AlphaTest 통합 및 Player/UI 작업 내역

작성일: 2026-07-10

## 1. 브랜치 통합 기준

- `AlphaTest`는 UI 브랜치를 기반으로 만들고 Player 변경을 병합하는 흐름으로 정리했다.
- Player/UI 초기화 순서는 `UI -> PlayerUIEventBridge -> Player`로 설정했다.
- `PlayerUIEventBridge`는 `InGamePrototype` 씬의 활성 오브젝트에 연결되어 있다.

## 2. Player 데이터와 HUD 연동

- 테스트 키 입력이 가짜 UI 수치가 아니라 실제 Player 데이터를 사용하도록 정리했다.
  - `2`: 실제 경험치 획득
  - `3`: 실제 레벨업 경험치 처리
  - `4`: 실제 `PlayerStatus.TakeDamage()` 호출
- 스킬 관련 기능은 이번 Player/UI 연동 수정 범위에서 제외했다.
- `PlayerUIEventBridge`는 HP, 목숨, 경험치, 레벨, 회복 아이템의 최신 값을 저장한다.
- HUD가 처음에는 비활성일 수 있으므로, `InGame` 화면으로 전환된 뒤 저장한 실제 Player 상태를 HUD 이벤트로 다시 전달한다.
  - 첫 게임 진입 시 HP는 `100 / 100`으로 표시되어야 한다.
  - 초기 이벤트가 HUD보다 먼저 발생해도 값이 유실되지 않도록 처리했다.

## 3. 목숨, 게임 오버, 체크포인트 부활

현재 구현 기준의 실제 목숨은 3개다.

| 실제 목숨 | HUD 활성 아이콘 | 사망 후 동작 |
| --- | --- | --- |
| 3 | 2개 | 첫 시작 상태 |
| 2 | 1개 | 게임 오버 후 LoadCheckpoint 가능 |
| 1 | 0개 | 게임 오버 후 LoadCheckpoint 가능 |
| 0 | 게임 오버 화면 | LoadCheckpoint 불가, 사망 상태 유지 |

- Player가 사망하면 목숨을 먼저 1 차감하고, 매번 게임 오버 화면을 띄운다.
- 기존 2초 자동 부활 호출은 제거했다. 자동으로 체크포인트로 이동하지 않는다.
- `LoadCheckpointButton`은 실제 `PlayerStatus.TryReviveAtCheckpoint()`을 호출한다.
  - 목숨이 남아 있을 때만 동작한다.
  - 활성 체크포인트가 있으면 해당 위치, 없으면 Player 시작 위치로 부활한다.
  - 체크포인트에 저장된 HP와 회복 아이템 수량을 복원한다.
  - 버튼으로 부활할 때 목숨을 추가 차감하지 않는다.
- 마지막 목숨에서 사망해 실제 목숨이 0이면 `LoadCheckpointButton`을 비활성화한다.
- `RestartButton`은 현재 씬을 다시 로드한다. 따라서 현재 스테이지의 Player 위치, 목숨, 체크포인트, 적, UI가 초기화된다.

## 4. 스테이지 간 영구 성장치

- 스테이지 클리어 시 실제 `PlayerStatus`의 영구 보정값을 `PrototypeProgressSnapshot`에 저장한다.
- 저장 대상: 최대 HP, 공격력, 이동속도, 공격속도, 쿨다운, 점프력, 최대 점프 횟수.
- 다음 스테이지 진입 시와 해당 스테이지에서 Restart할 때, 그 스테이지 시작 스냅샷의 보정값을 실제 Player에게 다시 적용한다.

예시:

1. 1스테이지에서 최대 HP 또는 이동속도를 획득한다.
2. 1스테이지 클리어 시 해당 성장치가 다음 스테이지 시작 데이터로 확정된다.
3. 2스테이지에서 Restart하면 2스테이지 중 얻은 진행은 초기화되지만, 1스테이지에서 가져온 성장치는 유지된다.

## 5. 수정된 주요 파일

- `Assets/00.Scripts/Data/Stat.cs`
  - 스탯 보정값을 스냅샷으로 저장/복원할 수 있는 API 추가.
- `Assets/00.Scripts/Player/PlayerLifeTracker.cs`
  - 사망 시 목숨 차감, 남은 목숨 확인 API 추가.
- `Assets/00.Scripts/Player/PlayerStatus.cs`
  - 자동 부활 제거, 버튼 전용 체크포인트 부활 API, 영구 스탯 스냅샷 API 추가.
- `Assets/01.Prefabs/Player.prefab`
  - 시작 목숨을 3으로 설정.
- `Assets/01.SB/others/PlayerUIEventBridge.cs`
  - 초기 HUD 상태 재전달, 목숨 UI 변환, 사망 시 게임 오버 전환 처리.
- `Assets/01.SB/Views/07 Game/GameOverView.cs`
  - LoadCheckpoint 버튼의 실제 활성/비활성 표시 처리.
- `Assets/01.SB/TestOnly/TestModule.cs`
  - Game Over 버튼이 실제 Player 부활과 현재 씬 재시작을 사용하도록 변경.
- `Assets/01.SB/TestOnly/PrototypeGameSession.cs`
  - 스테이지 간 영구 Player 스탯 스냅샷 보관.

## 6. 검증 상태

- `dotnet build Assembly-CSharp.csproj --no-restore` 성공.
  - 오류 0개.
  - 기존 경고 29개는 유지.
- Unity 배치 컴파일은 프로젝트가 이미 Unity에서 열려 있어 동시 실행 제한으로 완료하지 못했다.
- Unity 창 Play Mode 자동 조작도 창 활성화 단계에서 시간 초과되어 실행하지 못했다.

## 7. Unity에서 수동 확인할 항목

1. Play 직후 HUD가 HP `100 / 100`, 활성 목숨 아이콘 2개로 표시되는지 확인.
2. 피해를 누적해 첫 사망: 게임 오버와 LoadCheckpoint 활성화 확인.
3. LoadCheckpoint 클릭: 시작 위치 또는 활성 체크포인트에서 부활하고, 활성 목숨 아이콘 1개인지 확인.
4. 두 번째 사망 후 LoadCheckpoint 클릭: 부활 후 활성 목숨 아이콘 0개인지 확인.
5. 세 번째 사망: 게임 오버 화면에서 LoadCheckpoint 버튼이 비활성화되고 자동 부활하지 않는지 확인.
6. 1스테이지에서 스탯을 올린 뒤 2스테이지에 진입하고 Restart: 이전 스테이지 성장치는 유지되며 현재 스테이지는 초기화되는지 확인.

## 8. Git 주의 사항

`Assets/01.SB` 경로의 일부 파일은 현재 Git에서 새 파일(`Untracked`) 상태다. GitHub Desktop에서 필요한 파일을 반드시 추가한 뒤 커밋해야 한다.
