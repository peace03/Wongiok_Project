# Grimoire UI 작업 인수인계

작성일: 2026-07-14  
프로젝트: `SP_UI_Test`  
프로젝트 경로: `C:\Users\305\Downloads\평일반 2025 PSB 수업\정규 수업\유니티\SP_UI_Test`

이 문서는 기존 `Grimoire_UI_Handoff_2026-06-16.md`를 대체하지 않는다. 기존 문서는 초기 UI 설계 기록으로 보존하고, 이 문서는 2026-07-14 기준 실제 UI/테스트 연동 상태를 이어받기 위한 최신 인계 문서다.

---

## 1. 반드시 지킬 작업 원칙

- UI View는 화면 표시와 사용자 요청 이벤트 발행만 담당한다.
- 플레이어 스탯 계산, 스킬 사용/쿨타임 계산, 저장/로드, 씬 전환 결정, 보스 사망 판정은 UI가 직접 결정하지 않는다.
- UI와 외부 시스템의 기본 연결은 `EventBus<T>`다.
- `Assets/01 Scripts` 바깥의 팀원 스크립트, 특히 `Assets/ParkSooA/` 및 `Assets/Resources/` 내부 스크립트는 수정하거나 수정 제안하지 않는다. 실제로 막히는 버그만 별도 보고한다.
- 정식 스킬 연동은 A 방식이 목표다. 즉, 스킬 시스템 Presenter가 UI 요청 이벤트를 수신하고 Model 데이터를 UI 이벤트로 발행한다. 현재는 그 API가 준비되지 않아 `TestOnly` 스크립트가 임시 원본 역할을 한다.
- Codex는 사용자가 명시적으로 허용한 경우를 제외하고 파일을 직접 수정하거나 생성하지 않는다. 일반적으로는 정확한 파일/메서드/Inspector 위치와 적용 내용을 안내만 한다.
- Unity Inspector에 연결되는 `[SerializeField]` 이름, MonoBehaviour 클래스명, 씬 참조는 함부로 변경하지 않는다.

---

## 2. 씬과 핵심 구조

### 씬 역할

| 씬 | 역할 | 주요 컴포넌트 |
| --- | --- | --- |
| `Assets/Scenes/TestUI.unity` | 타이틀, 챕터 선택, 챕터 타이틀 카드, 로딩 흐름 테스트 | `Bootstrapper`, `UIManager`, `PopupManager`, `ScreenFader`, `UIBridgeTest`, `PrototypeSceneBridge` |
| `Assets/Scenes/InGamePrototype.unity` | 인게임 HUD, Pause, LevelUp, GameOver, GameClear, BossHUD 통합 테스트 | `Bootstrapper`, `UIManager`, `PopupManager`, `ScreenFader`, `UIInputBridge`, `PrototypeTestScene`, `TestModule` |

`UIManager`는 씬 간 `DontDestroyOnLoad`를 사용하지 않는다. 각 씬이 자기 Canvas와 Manager를 새로 가진다. 씬 사이에 유지해야 하는 테스트 진행 데이터는 정적 클래스 `PrototypeGameSession`이 담당한다.

### UI 핵심 컴포넌트

| 컴포넌트 | 책임 |
| --- | --- |
| `UIManager` | Screen/Overlay/HUD 표시 상태, Popup 상태에 따른 입력 차단, Pause/LevelUp/Cutscene TimeScale 정책 |
| `PopupManager` | Confirm/Alert 팝업 표시, `UISetPopupStateEvent` 발행 |
| `ScreenFader` | `UIFadeEvent` 기반 전체 화면 페이드 |
| `UIInputBridge` | ESC, Q/E 등 UI 입력을 현재 UI 상태에 맞는 EventBus 요청으로 변환 |
| `UIViewBase` | `CanvasGroup` 기반 Show/Hide 공통 기반 |
| `CommonButtonView` | 버튼 콜백, 라벨, 선택/비활성/hover/click 시각·사운드 공통 처리 |

### 테스트 전용 책임 분리

| 스크립트 | 현재 역할 |
| --- | --- |
| `PrototypeGameSession` | Play 중 씬 전환을 넘는 정적 진행 상태. 최고 클리어 챕터, 현재 챕터, 저장 데이터 여부, 커밋 성장 스냅샷, 챕터 시작 스냅샷, 체크포인트 스냅샷, 다음 타이틀 카드 예약을 보관 |
| `PrototypeSceneBridge` | TestUI에서 새로하기/이어하기, 챕터 입장, TitleCard 표시, 테스트 로딩 시간 후 InGame 씬 로드 |
| `PrototypeTestScene` | 실제 `BaseSkillData`를 UI 데이터로 변환해 Pause/HUD에 발행. 테스트용 스킬 장착/해제/교체, 레벨업 선택, HUD 쿨타임 상태 처리 |
| `TestModule` | 숫자/A/S/D 테스트 입력, 플레이어·보스 테스트 상태, GameOver/GameClear 요청 처리, 세션 스냅샷 적용/저장 |
| `UIBridgeTest` | TestUI의 단순 UI 테스트 보조. Title 흐름은 `PrototypeSceneBridge`가 단독으로 담당해야 한다. |

---

## 3. 이벤트 구조

이벤트는 `Assets/01 Scripts/UIEvent/`에 역할별 파일로 분리되어 있다. 타입명과 `EventBus<T>.Publish(...)` 사용 방식은 유지한다.

| 파일 | 주요 내용 |
| --- | --- |
| `UIEvent_Screen.cs` | Title, ChapterSelect, ChapterTitleCard, Loading, GameOver, ChapterClear, `UIChangeScreenEvent`, `UISetChapterProgressEvent` |
| `UIEvent_Overlay.cs` | Overlay 열기/닫기, Pause, Cutscene, LevelUp, Pause 스킬 표시/드래그 요청, 임시 `RefreshUIEventT` |
| `UIEvent_HUD.cs` | Player HUD와 Boss HUD 데이터. `UIPlayerSkillSlotData`는 키/레벨/쿨타임/사용 가능 상태를 포함 |
| `UIEvent_Popup.cs` | Confirm/Alert 표시와 Popup 상태 |
| `UIEvent_Common.cs` | `UIResetEvent`, `UIFadeEvent` |

### 자주 쓰는 흐름

```text
외부 시스템 / TestModule
  -> UI 데이터 이벤트 발행
  -> UI View가 표시 갱신

UI 버튼/드래그/카드 선택
  -> 요청 이벤트 발행
  -> GameFlow / Skill Presenter / TestOnly가 처리
  -> 최신 UI 데이터 이벤트를 다시 발행
```

Pause 스킬탭의 임시 연동은 `RefreshUIEventT`를 `PauseSkillPageView`가 직접 구독한다. 중간 `SkillUIEventBridge`는 사용하지 않는다.

---

## 4. 구현된 UI와 현재 연결 상태

### 메뉴/화면

- `TitleView`
  - 새로하기, 이어하기, 게임 종료 요청을 발행한다.
  - `UISetTitleSaveStateEvent`로 이어하기 활성 여부를 받는다.
  - 저장 데이터가 있는 새로하기는 Confirm 후 기존 데이터를 초기화하는 흐름이다.
- `ChapterSelectView` / `ChapterListView`
  - 챕터 선택, 보스 정보 패널 표시, 입장/뒤로가기 요청을 담당한다.
  - 리스트 내부 텍스트는 사용하지 않고 Sprite Image 표시가 기준이다.
  - 상태: `Cleared`, `Playable`, `Locked`.
  - `UISetChapterProgressEvent(highestClearedChapterId)` 기준으로 `<=`는 Cleared, `+1`은 Playable, 나머지는 Locked가 된다.
  - 플레이 가능 항목은 긴 버튼과 비율이 다르므로 `CommonVisualRoot`와 `PlayableVisualRoot`를 분리하는 구조가 권장된다.
- `ChapterTitleCardView`
  - 챕터 데이터 표시, 계속하기, 뒤로가기 요청을 담당한다.
  - `PrototypeSceneBridge`는 현재 `UIChangeScreenEvent(ChapterTitleCard)` 후 `UISetChapterTitleCardEvent`를 발행한다.
- `LoadingView`
  - 진행률/문구/회전 아이콘을 표시한다.
  - `PrototypeSceneBridge.testLoadingTime`으로 테스트 로딩 시간을 조절한다.
- `GameOverView`
  - 처음부터 다시 읽기, 책갈피부터 다시 읽기, 메인 메뉴 요청을 발행한다.
  - 책갈피 버튼은 데이터가 없어도 클릭 가능해야 하며, 실제 데이터 없음 판정과 Alert는 `TestModule`이 담당한다.
- `GameClearView`
  - 다음 이야기, 메인 메뉴, 게임 종료 요청을 발행한다.
  - 게임 종료는 Confirm 후 요청 이벤트를 발행한다.
- `CutsceneView`
  - `VideoPlayer + RenderTexture + RawImage` 구조. 스킵 요청은 Confirm을 거친다.

### HUD

- `PlayerHUDView`
  - 레벨, EXP, HP, 목숨, 액티브 스킬 3칸, 회복 아이템을 표시한다.
  - 스킬 슬롯의 `UIPlayerSkillSlotData`는 `Icon`, `KeyText`, `Level`, `CooldownProgress`, `IsAvailable`을 가진다.
  - `KeyText`는 스킬명이 아니라 입력 키 `A/S/D`를 표시한다.
  - 쿨타임 어두운 이미지는 Vertical Filled Image로 사용한다. `CooldownProgress`는 사용 직후 `1`, 완료 시 `0`이다.
  - 준비 완료 flash를 위한 `skillReadyFlashImages`와 transition 감지 로직이 추가되어 있다.
- `BossHUDView`
  - `UISetBossHudDataEvent`로 이름/HP, `UISetBossHudVisibleEvent`로 표시 여부를 받는다.
  - HP Fill은 `Image.fillAmount`, 위험 표시는 `hpWarningRatio` 이하에서 활성화한다.

### Pause

- `PauseView`
  - 전체 현황 / 스킬 / 옵션 탭을 관리한다.
  - ESC로 열고 닫는 정책이며 Popup이 열려 있으면 `UIInputBridge`가 단축키를 무시한다.
- `PauseStatusPageView`
  - 레벨, EXP, HP, 목숨, 장착 스킬 요약을 `UISetPauseStatusEvent`에서 표시한다.
  - PlayerHUD와 같은 값을 `TestModule.PublishPlayerState()`가 함께 발행한다.
- `PauseSkillPageView`
  - 장착 슬롯 3개는 고정.
  - 보유 스킬은 `ownedSkillItemPrefab + CustomObjectPool + ownedSkillContentRoot` 기반 동적 목록.
  - `RefreshUIEventT`와 `UISetPauseSkillPageEvent`를 수신한다.
  - 보유 스킬과 장착 슬롯 모두 이름/설명은 기본 숨김이며, hover 툴팁에서만 표시한다.
- 스킬 드래그
  - `PauseSkillDragView`: 프리뷰 아이콘, 원본 반투명, 요청 이벤트 발행.
  - `PauseSkillSlotDropView`: 장착 슬롯 드롭.
  - `PauseSkillOwnedDropView`: 장착 슬롯에서 보유 목록 영역으로 놓을 때 해제 요청.
  - UI는 내부 스킬 상태를 확정 변경하지 않고 요청 이벤트만 발행한다. `PrototypeTestScene`이 현재는 테스트 결과를 갱신해 다시 발행한다.
- 캐릭터 프리뷰
  - `PauseCharacterPreviewView`는 `[ / ]` 회전, 휠 줌, 확대 상태의 좌클릭 드래그 Pan, Reset을 담당한다.
  - Pause 스킬 탭 재진입과 Reset 버튼 클릭 시 회전/줌/Pan 초기화.
  - `CharacterPreviewWorld -> PreviewCamera -> RenderTexture -> RawImage` 구조다.

### LevelUp

- `LevelUpView`는 최대 3개의 `SkillSelectCardView`를 표시한다.
- 후보가 없으면 `EmptyOptionObject`의 계속 버튼으로 Overlay를 닫는다.
- 테스트 기준 후보는 현재 장착된 액티브 스킬만 대상이다.
- 카드 클릭은 `UILevelUpSkillSelectedEvent`만 발행한다. 테스트에서는 `PrototypeTestScene`이 해당 SkillId 레벨을 올리고 `RefreshUIEventT`를 재발행한다.

---

## 5. 실제 스킬 Resource를 이용한 임시 연동

실제 스킬 데이터는 `Resources.LoadAll<BaseSkillData>("Datas/Skills")`에서 읽는다. `PrototypeGameSession`이 저장하는 SkillId와 실제 리소스를 결합해 `UIPauseSkillInfoData`를 만든다.

초기 세션 데이터:

| SkillId | 스킬 | 초기 상태 |
| --- | --- | --- |
| 1001 | Magnum | 슬롯 0, 레벨 1 |
| 1002 | Rifle | 슬롯 1, 레벨 1 |
| 1003 | Sniper | 슬롯 2, 레벨 1 |
| 1004 | Shotgun | Chapter 1 첫 클리어 후 미장착 보유 목록 해금 |
| 1005 | SMG | Chapter 1 첫 클리어 후 미장착 보유 목록 해금 |

### 챕터/성장/체크포인트 정책

```text
새 게임
  -> Chapter 1만 Playable
  -> Magnum/Rifle/Sniper만 존재

Chapter 1 클리어
  -> 현재 성장 상태를 committedSnapshot에 커밋
  -> HighestClearedChapterId = 1
  -> Shotgun/SMG를 미장착 보유 스킬로 추가
  -> Chapter 2가 Playable

다음 챕터
  -> 현재 성장 상태를 유지한 채 Chapter 2 시작 스냅샷 생성

메인 메뉴 또는 챕터 처음부터 다시 읽기
  -> 체크포인트 삭제
  -> 미커밋 성장 폐기
  -> 해당 챕터 진입 당시의 커밋 성장 상태로 복구

체크포인트 부활
  -> 체크포인트 스냅샷으로 복구
  -> 목숨 1개 소비
  -> HP는 최대치
```

`PrototypeGameSession`은 현재 Play 중에만 유지되는 정적 세션이다. Unity Play Mode를 종료하면 `[RuntimeInitializeOnLoadMethod]`로 초기화되며 디스크 JSON 저장은 아직 없다.

---

## 6. 테스트 입력표

`InGamePrototype`의 `TestModule.Update()`가 입력을 받는다.

| 입력 | 테스트 동작 | 주요 결과 |
| --- | --- | --- |
| `1` | 현재 챕터 클리어 | ChapterClear 화면, 진행도 커밋 |
| `2` | EXP 획득 | HUD/Pause EXP 갱신 |
| `3` | 레벨업 | 장착 스킬 기반 LevelUp Overlay |
| `4` | 플레이어 HP 감소 | HP 갱신, 0이면 GameOver |
| `5` | 즉시 GameOver | GameOver 화면 |
| `6` | 보스 등장 | BossHUD 표시 |
| `7` | 보스 HP 감소 | BossHUD HP 갱신, 0이면 클리어 |
| `8` | 보스 즉시 처치 | BossHUD 숨김 후 ChapterClear |
| `9` | ChapterSelect 표시 | 세션 진행도 기준 상태 갱신 확인 |
| `0` | 테스트 상태 초기화 | 세션 초기화 후 InGame 상태 복구 |
| `.` | 체크포인트 저장 | 현재 성장/스킬 스냅샷 저장 |
| `A` | HUD 슬롯 0 스킬 사용 테스트 | `TestPlayerSkillUsedEvent(0)` |
| `S` | HUD 슬롯 1 스킬 사용 테스트 | `TestPlayerSkillUsedEvent(1)` |
| `D` | HUD 슬롯 2 스킬 사용 테스트 | `TestPlayerSkillUsedEvent(2)` |
| `ESC` | InGame Pause 열기/닫기, Cutscene 스킵 요청 | `UIInputBridge` 정책 |
| `Q / E` | Pause 탭 좌우 이동 | 캐릭터 회전 용도가 아님 |
| `[ / ]` | Pause 스킬탭 캐릭터 프리뷰 회전 | `PauseCharacterPreviewView` |
| 마우스 휠 | 프리뷰 영역에서 줌 | 프리뷰 영역 Hover 상태에서만 |
| 좌클릭 드래그 | 줌 인 상태 프리뷰 Pan | Pan 허용 범위 내에서만 |

주의: 현재 `SkillSystemController`도 A/S/D를 실제 스킬 입력으로 수신할 수 있다. UI 쿨타임 테스트만 할 때는 해당 Controller를 비활성화해야 실제 Projectile 실행과 충돌하지 않는다.

---

## 7. Unity Inspector 핵심 연결

### TestUI

- `Managers/PrototypeSceneBridge`
  - `inGameSceneName = InGamePrototype`
  - `testLoadingTime`: 테스트 로딩 시간
  - `chapterTitleCards`: Chapter 1/2 각각 ID, title, subtitle, description, thumbnail, background 연결
- `UIManager.Screen Bindings`
  - `Title`, `ChapterSelect`, `ChapterTitleCard`, `Loading`
- `PopupManager`
  - Confirm/Alert Popup View 연결
- ChapterList 각 항목
  - `ChapterListView`에 상태별 Visual Root와 상태 Sprite/Marker 연결
  - 리스트 텍스트는 꺼둔다. 좌측 보스 정보 영역만 Text 사용.

### InGamePrototype

- `UIManager.Screen Bindings`
  - `GameOver -> GameOverView`
  - `ChapterClear -> GameClearView`
  - 필요 시 `Loading -> LoadingView`
- `UIManager.Overlay Bindings`
  - `Pause -> PauseView`
  - `LevelUp -> LevelUpView`
  - `Cutscene -> CutsceneView`
- `UIManager`
  - `playerHudView -> PlayerHUDView`
  - `bossHudView -> BossHUDView`
- `PauseSkillPageView`
  - `ownedSkillContentRoot`, `ownedSkillItemPrefab`, `rootCanvas`, `dragPreviewRoot`, `dragPreviewIconImage` 필수
  - 장착 슬롯 3개에 `PauseSkillInfoView`, `PauseSkillDragView`, `PauseSkillSlotDropView`를 각각 순서 `0/1/2`로 연결
  - `OwnedSkills` 영역은 `PauseSkillOwnedDropView`와 Raycast 가능한 Image 필요
  - hover Root/Texts와 캐릭터 프리뷰/ResetButton 연결
- `LevelUpView`
  - title/guide Text, 카드 3개, empty Root, empty Button 연결
- `PlayerHUDView`
  - 스킬 아이콘, cooldown dark image, ready flash image를 슬롯 A/S/D 순서로 각각 연결
  - cooldown Image: `Filled / Vertical / Top / Fill Amount 0 / Raycast Target Off`
  - flash Image: icon 위에 배치, alpha 0, Raycast Target Off
- `BossHUDView`
  - bossName, hpFill, hpText, warning object 연결
  - hpFill: `Filled / Horizontal / Left`

### CharacterPreview 레이어/카메라

프리뷰 모델이 인게임 월드에 보이지 않게 하려면 아래를 적용한다.

- `CharacterPreview` Layer 생성.
- `CharacterPreviewWorld`, CameraPivot, PreviewCamera, Modelroot, 모델 자식 전체를 이 Layer로 변경.
- Main Camera Culling Mask에서 `CharacterPreview` 해제.
- PreviewCamera Culling Mask는 `CharacterPreview`만 체크.
- `PauseCharacterPreviewView.previewWorldRoot`에 `CharacterPreviewWorld` 연결.
- 스킬탭 OnEnable에서는 Preview World를 켜고, OnDisable에서는 끄는 활성 제어가 적용되어 있어야 한다.

---

## 8. 남은 검증과 알려진 문제

아래 항목은 완료로 단정하지 말고 다음 프로젝트에서 우선 Play Mode로 재현/검증한다.

1. **ChapterTitleCard 뒤로가기 진행도 반영**
   - 재현: Chapter 1 클리어 -> ChapterClear의 다음 챕터 -> Chapter 2 TitleCard -> 뒤로가기 -> ChapterSelect.
   - 기대: 즉시 Chapter 1 Cleared, Chapter 2 Playable.
   - 이전에는 Title 화면으로 돌아가 이어하기를 다시 눌러야 반영되는 현상이 있었다.
   - 확인 지점: `ChapterTitleCardView`의 Back 요청을 받는 쪽에서 `UIChangeScreenEvent(ChapterSelect)` 이후 `UISetChapterProgressEvent(PrototypeGameSession.HighestClearedChapterId)`를 다시 발행하는지.

2. **PlayerHUD 쿨타임 A/S/D 위치와 준비 완료 flash**
   - 이전 증상: S/D 사용에도 A 위치의 Dark/Flash가 보였다.
   - 씬 구조상 원인으로 확인된 배치: `CooldownDarkImage_S`, `ReadyFlashImage_S`는 S 슬롯 기준 `(100, 100)`, D는 `(0, 200)` 오프셋이라 A 영역에 겹쳤다.
   - Inspector에서 S/D의 Cooldown/Flash Image `Anchored Position = (0, 0)`, 크기 100x100, Scale 1인지 재확인한다.
   - Child 순서는 Icon -> CooldownDark -> ReadyFlash가 되게 하여 Flash가 가려지지 않게 한다.

3. **실제 스킬 실행 NullReference는 UI 외부 문제**
   - 실제 A/S/D 입력은 `SkillSystemController`도 `StartedPressSkillSlot`을 발행한다.
   - 현재 확인된 예외 경로: `ActiveSkillExecuter.ProjectileRoutine()`에서 `bulletFactory.GetBullet()` 관련 NullReference.
   - Projectile factory, bullet pool/prefab, execute position 등 팀원 스킬 실행 환경 연결을 스킬 담당자에게 확인해야 한다.
   - UI 쿨타임 화면만 검증할 때는 `SkillSystemController`를 비활성화하고 `TestPlayerSkillUsedEvent` 흐름만 사용한다.

4. **캐릭터 프리뷰 인게임 숨김**
   - Layer 분리와 `previewWorldRoot` OnEnable/OnDisable 활성 제어가 실제 씬에 적용되었는지 검증 필요.
   - 기대: 인게임 Main Camera에는 모델이 보이지 않고 Pause 스킬탭 RawImage에서만 보인다.

5. **한글 깨짐 표기**
   - PowerShell 출력에서 일부 한글 문자열/주석이 깨져 보였다.
   - Unity/IDE에서 정상이라면 터미널 표시 문제일 수 있다. 실제 파일이 깨졌는지는 Unity Console과 Inspector 표시로 확인한다.

6. **테스트 전용 코드의 정식 교체 시점**
   - `TestModule`, `PrototypeTestScene`, `PrototypeGameSession`, `PrototypeSceneBridge`는 정식 GameFlow/Save/Skill 연동 전까지만 유지하는 테스트 계층이다.
   - 정식 전환 시 View 코드를 바꾸기보다, 외부 시스템이 동일 UI 이벤트를 발행하도록 교체한다.

---

## 9. 다음 프로젝트에서 우선 확인할 순서

1. Unity에서 `TestUI`와 `InGamePrototype`을 열고 Missing Script/Inspector None이 없는지 확인한다.
2. `dotnet build Assembly-CSharp.csproj --no-restore`로 현재 컴파일 상태를 확인한다.
3. `TestUI` Play: 새로하기 -> Chapter 1 입장 -> TitleCard -> Loading -> InGame 흐름을 확인한다.
4. `InGamePrototype` Play: PlayerHUD, ESC Pause, Pause 전체 현황/스킬탭, 동적 보유 스킬 목록을 확인한다.
5. A/S/D 쿨타임 UI 테스트는 `SkillSystemController`를 끈 상태와 켠 상태를 분리해서 확인한다.
6. `1` 또는 `8`으로 Chapter 1 클리어 후 Chapter 2 해금/재입장/메인 메뉴/이어하기 흐름을 검증한다.
7. `.` 체크포인트, 사망, GameOver의 책갈피/처음부터 다시 읽기, 성장/목숨 복원 정책을 확인한다.
8. CharacterPreview 레이어와 PreviewCamera Culling Mask를 확인한다.
9. 정식 스킬 담당자 API가 준비되면 임시 `RefreshUIEventT` 발행을 Presenter의 정식 UI 데이터 발행으로 교체한다.

---

## 10. 빠른 파일 지도

```text
Assets/01 Scripts/
├─ Managers/
│  ├─ UIManager.cs
│  └─ PopupManager.cs
├─ UIEvent/
│  ├─ UIEvent_Screen.cs
│  ├─ UIEvent_Overlay.cs
│  ├─ UIEvent_HUD.cs
│  ├─ UIEvent_Popup.cs
│  └─ UIEvent_Common.cs
├─ Views/
│  ├─ 01 Title/              TitleView, CutsceneView
│  ├─ 02 Chapter/            ChapterList, ChapterSelect, ChapterTitleCard
│  ├─ 03 HUD/                PlayerHUD, BossHUD
│  ├─ 04 Pause/              Pause, Status, Skill, Drag, Hover, CharacterPreview
│  ├─ 05 Popup/              Confirm, Alert
│  ├─ 06 System/             CommonButton, Loading
│  └─ 07 Game/               LevelUp, SkillSelectCard, GameOver, GameClear
├─ others/
│  ├─ UIInputBridge.cs
│  ├─ PlayerUIEventBridge.cs
│  └─ ScreenFader.cs
└─ TestOnly/
   ├─ PrototypeGameSession.cs
   ├─ PrototypeSceneBridge.cs
   ├─ PrototypeTestScene.cs
   ├─ TestModule.cs
   └─ UIBridgeTest.cs
```

이 문서의 핵심은 현재 UI를 새로 재구현하는 것이 아니라, 정식 GameFlow/Player/Skill 시스템이 기존 EventBus 계약을 발행하도록 연결하는 데 있다.
