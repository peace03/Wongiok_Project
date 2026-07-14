# Grimoire UI 작업 인수인계 노트

작성일: 2026-06-16  
프로젝트: `SP_UI_Test`  
위치: `C:\Users\305\Downloads\평일반 2025 PSB 수업\정규 수업\유니티\SP_UI_Test`

---

## 1. 작업 원칙

- 이 프로젝트의 UI는 `EventBus + Bootstrapper + Single Entry Point Reset` 흐름을 기준으로 설계한다.
- UI 스크립트는 실제 게임 로직을 직접 처리하지 않는다.
- 저장/로드, 씬 전환, 플레이어 스탯 계산, 스킬 계산, 게임 종료 같은 실제 처리는 GameFlow/Player/Save 등 외부 시스템이 담당한다.
- UI는 화면 표시와 버튼 입력 전달만 담당한다.
- 다른 시스템과 직접 참조로 강하게 연결하지 않고, 가능하면 `EventBus<T>.Publish(...)`로 요청을 발행한다.
- 앞으로 스크립트 주석은 한국어로 작성한다.
- 기존 사용자가 정한 규칙: Codex는 파일을 직접 생성/수정하지 않고, 사용자가 직접 반영한다. 이 문서는 예외적으로 새 컴퓨터 인수인계를 위해 요청받아 작성했다.

---

## 2. 현재 전체 진척률

현재 상태 기준 추정 진척률:

- UI 코어 구조: 약 85~90%
- 팝업/페이드/공통 버튼: 약 85%
- 타이틀 → 챕터 선택 → 챕터 표지 → 로딩 흐름 UI: 약 75~80%
- 인게임 HUD 기반: 약 65~70%
- Pause 기본 Overlay: 약 55~60%
- Pause 내부 페이지/스킬 표시: 약 20~30%
- 실제 플레이어/게임 실행 로직 연동 준비: 약 35~45%
- 보스 제외 프로토타입 연동 테스트 가능성: 약 60% 전후

핵심 판단:

- UI 구조와 EventBus 기반은 많이 잡혀 있다.
- 아직 부족한 부분은 실제 GameFlow, 플레이어 상태, 씬 전환, 입력 시스템과의 연결이다.
- 이틀 뒤 보스 제외 연동 테스트를 목표로 한다면 새 UI 기능을 많이 늘리기보다, 플레이어/게임 흐름과 EventBus를 연결하는 쪽에 집중해야 한다.

---

## 3. 완료된 주요 구조

### 공통/기반

- `UIViewBase`
  - 모든 View의 Show/Hide, CanvasGroup, Dim 처리 기반.
- `UIManager`
  - `IInitializable` 구현.
  - Bootstrapper에서 초기화된다.
  - Screen / Overlay / Popup / HUD 상태 관리.
  - `UIChangeScreenEvent`, `UIOpenOverlayEvent`, `UICloseOverlayEvent`, `UISetPopupStateEvent`, `UIResetEvent` 등을 수신한다.
  - 현재 TimeScale 제어도 포함되어 있지만, 추후 별도 시간 제어 매니저로 분리 가능하게 설계한다.
- `Bootstrapper`
  - `IInitializable` 구현체를 찾아 `Priority` 순서대로 `Init()` 호출.
  - 현재 UIManager → PopupManager → ScreenFader 순서로 초기화되는 구조.
- `ServiceLocator`
  - 팀 공통 초기화/서비스 등록 구조.
- `EventBus<T>`
  - struct 이벤트 기반 공통 이벤트 버스.

### UI 상태 enum

- `UIScreenState`
  - `Title`, `ChapterSelect`, `ChapterTitleCard`, `Loading`, `InGame`, `GameOver`, `ChapterClear` 등.
- `UIOverlayState`
  - `Cutscene`, `LevelUp`, `Pause` 등.
  - 기존 Dialogue는 기획 변경으로 Cutscene 기준으로 변경됨.
- `UIPopupType`
  - `None`, `Alert`, `Confirm`.
- `UIInputBlockType`
  - `None`, `GameplayOnly`, `All`.

---

## 4. 구현된 View/Manager 현황

현재 `Assets/01 Scripts` 기준 주요 스크립트:

- `UIViewBase.cs`
- `UIManager.cs`
- `PopupManager.cs`
- `ConfirmPopupView.cs`
- `AlertPopupView.cs`
- `ScreenFader.cs`
- `CommonButtonView.cs`
- `TitleView.cs`
- `LoadingView.cs`
- `ChapterListView.cs`
- `ChapterSelectView.cs`
- `ChapterTitleCardView.cs`
- `PlayerHUDView.cs`
- `PauseView.cs`
- `UIBridgeTest.cs`
- `UIEvent.cs`

### PopupManager / Popup Views

- `PopupManager`
  - Confirm/Alert 팝업 표시 담당.
  - `UIShowConfirmPopupEvent`, `UIShowAlertPopupEvent`를 구독한다.
  - 팝업 열림/닫힘 시 `UISetPopupStateEvent`를 발행해 UIManager의 입력 차단 정책과 연동한다.
- `ConfirmPopupView`
  - 확인/취소 버튼 표시 및 콜백 호출.
- `AlertPopupView`
  - 단일 확인 버튼 알림 팝업.

### TitleView

- 새 게임, 이어하기, 게임 종료 버튼 담당.
- 실제 새 게임/이어하기/종료는 직접 처리하지 않고 EventBus 요청만 발행한다.
- 필요한 주요 이벤트:
  - `UISetTitleSaveStateEvent`
  - `UITitleNewGameRequestedEvent`
  - `UITitleContinueRequestedEvent`
  - `UITitleExitRequestedEvent`

### LoadingView

- 로딩 메시지, 진행률, 회전 아이콘 표시.
- `UISetLoadingProgressEvent` 수신.
- 회전은 `Time.unscaledDeltaTime` 기준.

### ChapterSelectView / ChapterListView

- 챕터 목록 표시 및 선택.
- `Locked` 챕터만 클릭 불가.
- `Playable`, `Cleared` 챕터는 클릭 가능하며 재입장 가능.
- 입장 버튼 클릭 시 Confirm 팝업 후 `UIChapterEnterRequestedEvent` 발행.
- 뒤로가기 버튼은 `UIChapterBackRequestedEvent` 발행.

### ChapterTitleCardView

- 챕터 입장 직전 표지 화면.
- `UISetChapterTitleCardEvent`로 데이터 갱신.
- 진행 버튼 클릭 시 `UIChapterTitleCardContinueRequestedEvent` 발행.

### PlayerHUDView

- 인게임 기본 HUD.
- 표시 대상:
  - 레벨
  - 경험치
  - HP
  - HP 경고 표시
  - 목숨
  - 스킬 슬롯
  - 회복 아이템
- 수신 이벤트:
  - `UISetPlayerLevelEvent`
  - `UISetPlayerExpEvent`
  - `UISetPlayerHpEvent`
  - `UISetPlayerLifeEvent`
  - `UISetPlayerSkillSlotsEvent`
  - `UISetPlayerHealItemEvent`
- 주의:
  - `UIPlayerSkillSlotData.KeyText`는 스킬 이름이 아니라 입력 키 표시용이다. 예: `A`, `S`, `D`.
  - 스킬 이름이 필요하면 별도 `SkillName` 필드를 추가하거나 Pause용 데이터 구조를 사용한다.

### PauseView

- 일시정지 Overlay의 기본 껍데기.
- 탭:
  - 전체 현황
  - 스킬
  - 옵션
- 버튼:
  - 계속하기
  - 메인 메뉴
  - 게임 종료
- 계속하기는 `UICloseOverlayEvent(UIOverlayState.Pause)` 발행.
- 메인 메뉴/게임 종료는 Confirm 팝업 후 각각 요청 이벤트 발행.
- 관련 이벤트:
  - `UIPauseMainMenuRequestedEvent`
  - `UIPauseQuitGameRequestedEvent`

---

## 5. 기획 변경/확정 사항

### 인게임 대사창 변경

- 기존 인게임 Dialogue UI는 폐기.
- 인게임 대사/연출은 영상 컷씬으로 대체 확정.
- 따라서 `UIOverlayState.Dialogue`가 아니라 `UIOverlayState.Cutscene` 기준으로 설계한다.

### 컷씬 스킵 정책

- 영상 컷씬 재생 중 ESC 입력 시 건너뛰기 Confirm 팝업을 띄운다.
- Confirm 팝업에는 해당 영상의 간략한 줄거리를 표시한다.
- 확인:
  - 영상 즉시 종료.
  - 다음 진행 단계로 이동.
- 취소:
  - 팝업만 닫고 영상 재개.
- 영상 재생/일시정지/종료 자체는 PopupManager가 처리하지 않는다.
- 컷씬 담당 시스템이 PopupManager 이벤트를 사용해 Confirm을 띄우고 콜백에서 처리한다.

### Pause 옵션 탭 변경

- 기존 옵션 탭의 저장하기 버튼은 기획 변경으로 제거.
- Pause 옵션 탭의 주요 버튼은 다음 중심으로 본다.
  - 메인 메뉴로 돌아가기
  - 게임 종료
- `책 갈피/저장하기`는 현재 구현 대상에서 제외.

---

## 6. 아직 미완성/다음 작업

### 가장 가까운 다음 작업

1. `PauseStatusPageView`
   - 일시정지 메뉴의 `전체 현황` 페이지.
   - `PauseView` 내부 페이지이므로 `UIViewBase` 상속 없이 `MonoBehaviour`로 작성하는 방향.
   - 표시 대상:
     - 레벨
     - EXP
     - HP
     - 목숨
     - 장착 중인 액티브 스킬 3개
     - 보유 패시브 스킬 목록
     - 캐릭터 표시 영역 placeholder

2. `PauseSkillInfoView`
   - Pause 페이지에서 스킬 하나를 표시하는 공통 View.
   - 아이콘, 이름, 레벨, 설명, 장착 여부 표시.
   - 클릭/드래그/교체 기능은 아직 넣지 않는다.

3. Pause 전체 현황 이벤트 추가
   - `UIPauseSkillInfoData`
   - `UISetPauseStatusEvent`

### 이후 작업 순서 추천

1. `PauseStatusPageView` / `PauseSkillInfoView`
2. `PauseSkillPageView` 기본 표시
3. ESC 입력 브릿지
   - InGame 상태에서 ESC → `UIOpenOverlayEvent(UIOverlayState.Pause)`
   - Pause 상태에서 ESC → `UICloseOverlayEvent(UIOverlayState.Pause)`
   - Popup이 떠 있으면 Popup 우선 처리
4. GameFlow 브릿지
   - Title New Game → ChapterSelect
   - Chapter Enter → ChapterTitleCard
   - ChapterTitleCard Continue → Loading → InGame
5. Player 상태 연동
   - HP/EXP/목숨/스킬/회복 아이템 변경 시 PlayerHUD 이벤트 발행
6. 컷씬 Overlay / 컷씬 스킵 Confirm 연동
7. GameOverView
8. ChapterClearView

---

## 7. 이틀 뒤 보스 제외 연동 테스트를 위한 최소 목표

보스 제외, 플레이어와 게임 실행 로직 연동 테스트만 한다면 최소 필요 항목은 다음과 같다.

### 필수

- Bootstrapper가 UIManager, PopupManager, ScreenFader를 정상 초기화.
- `UIManager` Screen Binding 연결.
  - Title
  - ChapterSelect
  - ChapterTitleCard
  - Loading
  - InGame
- `UIManager` Overlay Binding 연결.
  - Pause
  - Cutscene은 가능하면 placeholder라도 준비
- `UIManager.playerHudView`에 PlayerHUDView 연결.
- PopupManager에 Confirm/Alert 연결.
- GameFlow 역할의 임시 브릿지 또는 실제 매니저 필요.
  - Title 요청 수신
  - Chapter 요청 수신
  - Loading 전환
  - InGame 전환
- Player 쪽에서 UI 이벤트 발행.
  - `UISetPlayerHpEvent`
  - `UISetPlayerExpEvent`
  - `UISetPlayerLifeEvent`
  - `UISetPlayerSkillSlotsEvent`
  - `UISetPlayerHealItemEvent`

### 선택

- PauseStatusPageView
- GameOverView
- ChapterClearView
- 컷씬 스킵 Confirm

### 테스트에서 제외 가능

- 보스 HUD
- 보스 HP
- 보스 패턴
- 정식 저장/로드
- 스킬 드래그 교체
- 3D 캐릭터 프리뷰 회전

---

## 8. 현재 주의할 점

### 1. 한글 인코딩

Codex 터미널 출력에서는 한글 주석/문자열이 깨져 보인다.  
Unity/IDE에서 정상 표시된다면 문제 없음.  
새 컴퓨터에서 깨져 보이면 파일 인코딩을 UTF-8로 통일하는 것을 추천한다.

### 2. `UIPlayerSkillSlotData.KeyText`

- `KeyText`는 스킬 이름이 아니다.
- 입력 키 표시용이다.
- 예: `A`, `S`, `D`, `Q`, `E`
- 스킬 이름은 별도 데이터가 필요하다.

### 3. PauseStatusPageView 이벤트 수신

`PauseStatusPageView`가 비활성 GameObject 아래에 있으면, 비활성 상태 동안 EventBus를 받지 못할 수 있다.  
따라서 Pause를 열 때 GameFlow/PlayerStatus 담당 시스템이 `UISetPauseStatusEvent`를 다시 발행하는 흐름이 안정적이다.

### 4. TimeScale

현재는 UIManager가 직접 `Time.timeScale`을 제어한다.  
나중에 담당자 회의 결과에 따라 별도 `GamePauseService`, `TimeScaleController`, `GameTimeManager`로 분리 가능하도록 유지한다.

### 5. UI는 게임 로직을 처리하지 않음

아래는 UI에서 직접 하지 않는다.

- 저장 파일 생성/삭제
- 씬 로딩 실제 처리
- 플레이어 HP 계산
- 경험치 계산
- 스킬 쿨타임 계산
- 챕터 진행 저장
- 게임 종료 직접 판단

---

## 9. 마지막 확인된 빌드 상태

최근 확인 결과:

```text
dotnet build Assembly-CSharp.csproj --no-restore
오류 0개
경고 11개
```

경고 대부분은 Unity Inspector에서 할당되는 `[SerializeField]` 필드가 C# 컴파일러 기준으로는 할당되지 않았다고 보는 경고다.  
현재 치명적인 컴파일 오류는 없다.

---

## 10. 새 컴퓨터에서 이어갈 때 추천 시작 순서

1. Unity로 프로젝트 열기.
2. 콘솔 컴파일 오류 확인.
3. `UIManager`, `PopupManager`, `ScreenFader`, `Bootstrapper`가 씬에 존재하는지 확인.
4. Canvas 아래 View 연결 확인.
5. `PauseView` Overlay Binding 확인.
6. 아직 없다면 `PauseStatusPageView`, `PauseSkillInfoView`부터 작성.
7. 다음으로 GameFlow 임시 브릿지 작성.
8. Player 쪽에서 HUD 이벤트를 발행하도록 연결.

---

## 11. 다음 구현 계획 요약

다음에 바로 이어서 할 일:

```text
PauseStatusPageView + PauseSkillInfoView
→ Pause 전체 현황 데이터 이벤트
→ ESC Pause 입력 브릿지
→ GameFlow 브릿지
→ PlayerHUD 실제 데이터 연동
→ 컷씬 스킵 Confirm
→ GameOver / ChapterClear
```

현재 상태는 UI 기반 공사가 꽤 진행된 상태다.  
앞으로는 “새 View를 더 많이 만드는 것”보다 “플레이어/게임 진행 시스템과 EventBus를 실제로 이어주는 것”이 테스트 성공에 더 중요하다.
