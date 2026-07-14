# GRIMOIRE UI Core Design 정리

작성 기준: 2026-06-04 대화 내용 및 업로드된 UI 기능 정리 / UI 기능 구현서 Part 1~3 / UI 와이어프레임 초안 기준  
목적: Codex에 올려서 `UIState`, `UIViewBase`, `UIManager` 구현을 시작하기 위한 설계 메모

---

## 1. 현재 기획 변경 확인 사항

### 1.1 일시정지 옵션 탭 변경

`일시정지 - 옵션 페이지`의 기존 버튼 구성은 다음과 같았다.

- 메인 메뉴로 나가기
- 책 갈피(저장하기)
- 책 덮기(끝내기)

하지만 최신 메모 기준으로 `책 갈피(저장하기)`는 폐기 처리되었다.

따라서 현재 UI 설계에서는 Pause Option 탭을 아래 중심으로 본다.

- 메인 메뉴로 나가기
- 책 덮기(끝내기)

단, 책 덮기 기능은 기획 변동 가능성이 높다고 메모되어 있으므로, 실제 구현에서는 버튼 View는 둘 수 있으나 동작은 `GameFlowManager` 또는 별도 게임 종료 담당 쪽에 위임한다.

---

### 1.2 Game Over 메인 메뉴 복귀 변경

기존 메모에서는 Game Over에서 `메인 화면으로 복귀` 선택 시 일부 진행 상황을 저장하는 방향이 있었다.

최신 메모 기준으로는 다음 방향으로 변경되었다.

- 메인 화면으로 이동
- 챕터 진행 사항은 모두 초기화
- `현재 챕터 자체를 재시작`과 다른 점은 메인 화면으로 이동된다는 점

따라서 UI 담당은 버튼 클릭 이벤트만 전달하고, 실제 챕터 진행 초기화는 `GameFlowManager`, `SaveSystem`, `ChapterProgressSystem` 쪽에서 처리해야 한다.

---

### 1.3 스킬 슬롯 변경 정책 유지

스킬 슬롯 변경은 다음 방향으로 유지된다.

- 일시정지 메뉴의 스킬 탭에서 상시 변경 가능
- 별도 스킬 장착 전용 화면은 만들지 않음
- 스킬 탭에 통합
- 보유 풀 → 슬롯 장착
- 슬롯 → 슬롯 스왑
- 슬롯 → 보유 풀로 빼기
- 회복 아이템 슬롯은 고정, 교체 불가

UI 담당은 드래그앤드롭 입력과 표시를 담당한다. 실제 스킬 장착/해제/스왑 검증과 데이터 변경은 스킬 담당 시스템에 요청해야 한다.

---

## 2. UI 담당 범위 재정의

### 2.1 UI 담당이 하는 것

| 구분 | UI 담당 범위 |
|---|---|
| 화면 상태 | 현재 UI 화면 / 오버레이 / 팝업 상태 관리 |
| 표시 | HP, EXP, 목숨, 스킬 슬롯, 챕터 목록, 팝업, 대화창 등 표시 |
| 입력 중계 | 버튼 클릭, ESC, Space, 방향키, 드래그앤드롭 입력을 시스템에 요청 |
| 공통 처리 | View Show/Hide, CanvasGroup 제어, UI 입력 차단 |
| HUD 처리 | InGame HUD 표시, Boss HUD 표시, dim 처리 |
| 일시정지 | Pause UI 열기/닫기, 게임 입력 차단, TimeScale 정지 요청 |

---

### 2.2 UI 담당이 하면 안 되는 것

| 구분 | 담당 아님 |
|---|---|
| 저장 | 저장 파일 생성, 삭제, 검증, 직렬화 |
| 스킬 | 스킬 데미지, 쿨타임 계산, 장착 가능 여부 확정, 레벨업 수치 적용 |
| 플레이어 | HP 계산, 회복량 계산, 사망 판정, 목숨 차감 |
| 보스 | 보스 HP 계산, 패턴, 그로기, 양날의 검, 사망 판정 |
| 씬 | 실제 SceneManager/Addressables 로딩 |
| 게임 흐름 | 챕터 진행 초기화, 다음 챕터 결정, 체크포인트 로드 |

---

## 3. UI 상태 설계 방향

UI 상태는 하나의 enum으로만 관리하면 나중에 복귀 처리가 꼬인다.

예시:

```text
InGame
	→ Dialogue
		→ Confirm Popup
			→ Popup Close
		→ Dialogue 복귀
	→ Dialogue Close
→ InGame 복귀
```

따라서 상태를 3개 계층으로 나눈다.

```text
1. UIScreenState
	기본 화면 상태

2. UIOverlayState
	기본 화면 위에 올라오는 차단형 UI

3. UIPopupType
	가장 위에 뜨는 확인/경고 UI
```

추가로 입력 차단 정도를 나타내는 `UIInputBlockType`을 둔다.

---

## 4. 상태 정의 코드

### 4.1 UIScreenState.cs

```csharp
namespace Grimoire.UI
{
	public enum UIScreenState
	{
		None = 0,

		Title,
		Prologue,
		ChapterSelect,
		ChapterTitleCard,
		Loading,

		InGame,

		GameOver,
		ChapterClear
	}
}
```

#### 의미

| 상태 | 의미 |
|---|---|
| `Title` | 타이틀 화면 |
| `Prologue` | 프롤로그 컷신/텍스트 화면 |
| `ChapterSelect` | 챕터 선택 화면 |
| `ChapterTitleCard` | 챕터 표지 화면 |
| `Loading` | 로딩 화면 |
| `InGame` | 실제 플레이 화면 + 기본 HUD |
| `GameOver` | 게임 오버 결과 화면 |
| `ChapterClear` | 챕터 클리어 결과 화면 |

---

### 4.2 UIOverlayState.cs

```csharp
namespace Grimoire.UI
{
	public enum UIOverlayState
	{
		None = 0,

		Dialogue,
		LevelUp,
		Pause
	}
}
```

#### BossHUD를 Overlay에 넣지 않는 이유

BossHUD는 입력을 막는 UI가 아니다.

- InGame HUD 위에 추가 표시된다.
- 인게임 HUD는 제거되지 않는다.
- 게임 입력을 차단하지 않는다.
- TimeScale을 건드리지 않는다.

따라서 BossHUD는 `Overlay`가 아니라 `HUD Layer`로 별도 관리한다.

---

### 4.3 UIPopupType.cs

```csharp
namespace Grimoire.UI
{
	public enum UIPopupType
	{
		None = 0,

		Alert,
		Confirm
	}
}
```

#### 예상 사용처

| 팝업 | 타입 |
|---|---|
| 저장 파일 손상 | Alert |
| 로딩 실패 | Alert |
| New Game 덮어쓰기 | Confirm |
| 게임 종료 확인 | Confirm |
| 프롤로그 스킵 확인 | Confirm |
| 대화 스킵 확인 | Confirm |
| 챕터 입장 확인 | Confirm |
| 메인 메뉴 복귀 확인 | Confirm |

---

### 4.4 UIInputBlockType.cs

```csharp
namespace Grimoire.UI
{
	public enum UIInputBlockType
	{
		None = 0,

		GameplayOnly,
		All
	}
}
```

#### 의미

| 타입 | 의미 |
|---|---|
| `None` | 게임 입력 가능 |
| `GameplayOnly` | 이동, 공격, 점프 등 게임플레이 입력 차단 |
| `All` | 모든 입력 무시. 로딩 화면 등에 사용 |

---

## 5. 입력 우선순위 정책

입력 우선순위는 다음 순서로 처리한다.

```text
Popup
	>
Blocking Overlay
	>
Screen UI
	>
Game Input
```

### 상태별 입력 차단

| 상태 | 입력 정책 |
|---|---|
| Popup | 팝업 버튼 입력 최우선 |
| Dialogue | Space/ESC 등 대화 입력만 허용, 게임 입력 차단 |
| LevelUp | 카드 선택 입력만 허용, ESC 무시 |
| Pause | Pause 메뉴 입력만 허용, 게임 입력 차단 |
| Loading | 모든 입력 무시 |
| InGame | 게임 입력 가능 |
| Title/ChapterSelect 등 | 메뉴 입력 가능, 게임 입력 차단 |

---

## 6. UIViewBase

모든 View의 공통 부모 클래스다.

### 6.1 역할

| 기능 | 설명 |
|---|---|
| `Show()` | View 표시 |
| `Hide()` | View 숨김 |
| `SetInteractable()` | 상호작용 가능 여부 설정 |
| `SetDimmed()` | dim 표현 |
| `OnShow()` | 자식 View 확장 지점 |
| `OnHide()` | 자식 View 확장 지점 |

---

### 6.2 UIViewBase.cs

```csharp
using UnityEngine;

namespace Grimoire.UI
{
	[RequireComponent(typeof(CanvasGroup))]
	public abstract class UIViewBase : MonoBehaviour
	{
		[SerializeField] private bool deactivateOnHide = true;

		private CanvasGroup canvasGroup;

		public bool IsVisible { get; private set; }

		protected virtual void Awake()
		{
			canvasGroup = GetComponent<CanvasGroup>();
		}

		public void Show()
		{
			if (IsVisible)
			{
				return;
			}

			gameObject.SetActive(true);

			IsVisible = true;
			SetCanvasState(1f, true, true);

			OnShow();
		}

		public void Hide()
		{
			if (!IsVisible)
			{
				return;
			}

			IsVisible = false;
			SetCanvasState(0f, false, false);

			OnHide();

			if (deactivateOnHide)
			{
				gameObject.SetActive(false);
			}
		}

		public void SetInteractable(bool isInteractable)
		{
			canvasGroup.interactable = isInteractable;
			canvasGroup.blocksRaycasts = isInteractable;
		}

		public void SetDimmed(bool isDimmed)
		{
			canvasGroup.alpha = isDimmed ? 0.45f : 1f;
		}

		private void SetCanvasState(float alpha, bool isInteractable, bool blocksRaycasts)
		{
			canvasGroup.alpha = alpha;
			canvasGroup.interactable = isInteractable;
			canvasGroup.blocksRaycasts = blocksRaycasts;
		}

		protected virtual void OnShow()
		{
		}

		protected virtual void OnHide()
		{
		}
	}
}
```

---

### 6.3 public / private 기준

| 메서드 | 접근 제한자 | 이유 |
|---|---|---|
| `Show()` | public | UIManager가 호출해야 함 |
| `Hide()` | public | UIManager가 호출해야 함 |
| `SetInteractable()` | public | UIManager 또는 Presenter가 제어할 수 있음 |
| `SetDimmed()` | public | Dialogue/LevelUp 등에서 HUD dim 처리 필요 |
| `SetCanvasState()` | private | 내부 구현 세부사항 |
| `OnShow()` | protected virtual | 자식 View 확장 지점 |
| `OnHide()` | protected virtual | 자식 View 확장 지점 |

---

## 7. UIManager

### 7.1 책임

UIManager는 UI 상태와 View 표시만 관리한다.

| 기능 | 처리 여부 |
|---|---|
| Current Screen 관리 | O |
| Current Overlay 관리 | O |
| Current Popup Type 관리 | O |
| View Show/Hide 호출 | O |
| PlayerHUD 표시/숨김 | O |
| BossHUD 표시/숨김 | O |
| HUD dim 처리 | O |
| 입력 차단 상태 제공 | O |
| TimeScale 정지/복구 | O, 단 추후 GamePauseService로 분리 가능 |
| 저장 파일 생성/삭제 | X |
| 스킬 장착 처리 | X |
| HP 계산 | X |
| 씬 로드 | X |

---

### 7.2 UIManager.cs

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Grimoire.UI
{
	public sealed class UIManager : MonoBehaviour
	{
		[Serializable]
		private struct ScreenBinding
		{
			public UIScreenState state;
			public UIViewBase view;
		}

		[Serializable]
		private struct OverlayBinding
		{
			public UIOverlayState state;
			public UIViewBase view;
		}

		[Header("Screen Views")]
		[SerializeField] private List<ScreenBinding> screenBindings = new();

		[Header("Overlay Views")]
		[SerializeField] private List<OverlayBinding> overlayBindings = new();

		[Header("HUD Views")]
		[SerializeField] private UIViewBase playerHudView;
		[SerializeField] private UIViewBase bossHudView;

		private readonly Dictionary<UIScreenState, UIViewBase> screenViews = new();
		private readonly Dictionary<UIOverlayState, UIViewBase> overlayViews = new();

		public UIScreenState CurrentScreenState { get; private set; } = UIScreenState.None;
		public UIOverlayState CurrentOverlayState { get; private set; } = UIOverlayState.None;
		public UIPopupType CurrentPopupType { get; private set; } = UIPopupType.None;

		public bool IsGameplayInputBlocked => GetInputBlockType() != UIInputBlockType.None;

		private void Awake()
		{
			InitializeScreenViews();
			InitializeOverlayViews();
			HideAllViews();
		}

		private void Start()
		{
			ChangeScreen(UIScreenState.Title);
		}

		public void ChangeScreen(UIScreenState nextState)
		{
			if (CurrentScreenState == nextState)
			{
				return;
			}

			CloseCurrentOverlay();

			HideScreen(CurrentScreenState);
			CurrentScreenState = nextState;
			ShowScreen(CurrentScreenState);

			ApplyHudPolicy();
			ApplyInputPolicy();
		}

		public void OpenOverlay(UIOverlayState overlayState)
		{
			if (overlayState == UIOverlayState.None)
			{
				return;
			}

			if (CurrentOverlayState == overlayState)
			{
				return;
			}

			if (CurrentOverlayState != UIOverlayState.None)
			{
				CloseCurrentOverlay();
			}

			CurrentOverlayState = overlayState;
			ShowOverlay(CurrentOverlayState);

			ApplyHudPolicy();
			ApplyInputPolicy();
		}

		public void CloseCurrentOverlay()
		{
			if (CurrentOverlayState == UIOverlayState.None)
			{
				return;
			}

			HideOverlay(CurrentOverlayState);
			CurrentOverlayState = UIOverlayState.None;

			ApplyHudPolicy();
			ApplyInputPolicy();
		}

		public void SetPopupState(UIPopupType popupType)
		{
			CurrentPopupType = popupType;
			ApplyInputPolicy();
		}

		public void SetBossHudVisible(bool isVisible)
		{
			if (bossHudView == null)
			{
				return;
			}

			if (isVisible)
			{
				bossHudView.Show();
				return;
			}

			bossHudView.Hide();
		}

		public UIInputBlockType GetInputBlockType()
		{
			if (CurrentPopupType != UIPopupType.None)
			{
				return UIInputBlockType.GameplayOnly;
			}

			if (CurrentScreenState == UIScreenState.Loading)
			{
				return UIInputBlockType.All;
			}

			if (CurrentOverlayState == UIOverlayState.Dialogue)
			{
				return UIInputBlockType.GameplayOnly;
			}

			if (CurrentOverlayState == UIOverlayState.LevelUp)
			{
				return UIInputBlockType.GameplayOnly;
			}

			if (CurrentOverlayState == UIOverlayState.Pause)
			{
				return UIInputBlockType.GameplayOnly;
			}

			if (CurrentScreenState != UIScreenState.InGame)
			{
				return UIInputBlockType.GameplayOnly;
			}

			return UIInputBlockType.None;
		}

		private void InitializeScreenViews()
		{
			screenViews.Clear();

			foreach (ScreenBinding binding in screenBindings)
			{
				if (binding.state == UIScreenState.None || binding.view == null)
				{
					continue;
				}

				if (screenViews.ContainsKey(binding.state))
				{
					Debug.LogWarning($"Duplicate screen binding found: {binding.state}");
					continue;
				}

				screenViews.Add(binding.state, binding.view);
			}
		}

		private void InitializeOverlayViews()
		{
			overlayViews.Clear();

			foreach (OverlayBinding binding in overlayBindings)
			{
				if (binding.state == UIOverlayState.None || binding.view == null)
				{
					continue;
				}

				if (overlayViews.ContainsKey(binding.state))
				{
					Debug.LogWarning($"Duplicate overlay binding found: {binding.state}");
					continue;
				}

				overlayViews.Add(binding.state, binding.view);
			}
		}

		private void HideAllViews()
		{
			foreach (UIViewBase view in screenViews.Values)
			{
				view.Hide();
			}

			foreach (UIViewBase view in overlayViews.Values)
			{
				view.Hide();
			}

			if (playerHudView != null)
			{
				playerHudView.Hide();
			}

			if (bossHudView != null)
			{
				bossHudView.Hide();
			}
		}

		private void ShowScreen(UIScreenState state)
		{
			if (!screenViews.TryGetValue(state, out UIViewBase view))
			{
				return;
			}

			view.Show();
		}

		private void HideScreen(UIScreenState state)
		{
			if (!screenViews.TryGetValue(state, out UIViewBase view))
			{
				return;
			}

			view.Hide();
		}

		private void ShowOverlay(UIOverlayState state)
		{
			if (!overlayViews.TryGetValue(state, out UIViewBase view))
			{
				return;
			}

			view.Show();
		}

		private void HideOverlay(UIOverlayState state)
		{
			if (!overlayViews.TryGetValue(state, out UIViewBase view))
			{
				return;
			}

			view.Hide();
		}

		private void ApplyHudPolicy()
		{
			bool shouldShowPlayerHud =
				CurrentScreenState == UIScreenState.InGame ||
				CurrentOverlayState == UIOverlayState.Dialogue ||
				CurrentOverlayState == UIOverlayState.LevelUp ||
				CurrentOverlayState == UIOverlayState.Pause;

			if (playerHudView != null)
			{
				if (shouldShowPlayerHud)
				{
					playerHudView.Show();
				}
				else
				{
					playerHudView.Hide();
				}
			}

			if (playerHudView != null)
			{
				bool shouldDimHud =
					CurrentOverlayState == UIOverlayState.Dialogue ||
					CurrentOverlayState == UIOverlayState.LevelUp;

				playerHudView.SetDimmed(shouldDimHud);
			}
		}

		private void ApplyInputPolicy()
		{
			bool shouldPauseGameplay =
				CurrentOverlayState == UIOverlayState.Dialogue ||
				CurrentOverlayState == UIOverlayState.LevelUp ||
				CurrentOverlayState == UIOverlayState.Pause;

			Time.timeScale = shouldPauseGameplay ? 0f : 1f;
		}
	}
}
```

---

## 8. 임시 테스트 View

아직 각 화면별 View가 없을 때는 아래 클래스를 붙여서 UIManager 연결을 테스트한다.

```csharp
namespace Grimoire.UI
{
	public sealed class EmptyUIView : UIViewBase
	{
	}
}
```

---

## 9. Unity 씬 구성 추천

```text
Canvas_UIRoot
├─ Screens
│  ├─ TitleView
│  ├─ PrologueView
│  ├─ ChapterSelectView
│  ├─ ChapterTitleCardView
│  ├─ LoadingView
│  ├─ GameOverView
│  └─ ChapterClearView
│
├─ HUD
│  ├─ PlayerHUDView
│  └─ BossHUDView
│
├─ Overlays
│  ├─ DialogueView
│  ├─ LevelUpView
│  └─ PauseMenuView
│
└─ Popups
   ├─ AlertPopupView
   └─ ConfirmPopupView
```

각 View 루트 오브젝트에는 공통으로 다음 컴포넌트를 붙인다.

```text
CanvasGroup
해당 View Script 또는 EmptyUIView
```

---

## 10. 첫 테스트 순서

```text
1. UIManager 오브젝트 생성
2. TitleView, LoadingView, InGame용 빈 View 오브젝트 생성
3. 각 View 루트에 CanvasGroup + EmptyUIView 붙이기
4. UIManager의 Screen Bindings에 연결
5. PlayerHUDView / BossHUDView도 EmptyUIView로 연결
6. Play 실행 시 Title만 뜨는지 확인
7. 테스트 버튼 또는 임시 코드에서 ChangeScreen(Loading) 호출
8. ChangeScreen(InGame) 호출
9. InGame에서 PlayerHUDView가 뜨는지 확인
10. OpenOverlay(Pause) 호출
11. PauseView가 뜨고 Time.timeScale이 0으로 바뀌는지 확인
12. CloseCurrentOverlay() 호출
13. Time.timeScale이 1로 복구되는지 확인
```

---

## 11. 다음 단계

현재 문서 기준 다음 구현 순서는 다음과 같다.

```text
1. PopupManager
2. ConfirmPopupView
3. AlertPopupView
4. ScreenFader
5. CommonButtonView
6. TitleView
7. LoadingView
8. ChapterSelectView
```

특히 PopupManager는 우선순위가 높다.

반복 사용처가 많기 때문이다.

- New Game 덮어쓰기
- Exit 확인
- 프롤로그 스킵 확인
- 대화 스킵 확인
- 챕터 입장 확인
- 메인 메뉴 복귀 확인
- 저장 파일 손상 안내
- 로딩 실패 안내

---

## 12. 주의할 점

### 12.1 UIManager에 게임 로직 넣지 말 것

UIManager는 화면 상태만 관리한다.

금지 예시:

```text
UIManager가 저장 파일 삭제
UIManager가 스킬 장착 배열 직접 수정
UIManager가 플레이어 HP 계산
UIManager가 보스 사망 판정
UIManager가 씬 로드 직접 처리
```

---

### 12.2 BossHUD는 Overlay가 아니다

BossHUD는 인게임 HUD에 추가되는 표시 UI다.

- 게임 입력 차단 안 함
- TimeScale 변경 안 함
- Pause/Dialog/LevelUp과 성격이 다름

---

### 12.3 TimeScale은 추후 분리 가능

현재는 UIManager에서 `Time.timeScale`을 직접 제어한다.

하지만 나중에 다음 요소가 생기면 별도 시스템으로 분리하는 게 좋다.

- 슬로우 모션
- 컷신 연출용 시간 제어
- 보스 패턴 일시정지
- 타임스케일 기반 애니메이션

예상 분리명:

```text
GamePauseService
TimeScaleController
GameTimeManager
```

---

## 13. 현재 설계 요약

```text
UIScreenState
	기본 화면 관리

UIOverlayState
	입력을 막는 오버레이 관리

UIPopupType
	최상단 팝업 관리

UIInputBlockType
	현재 입력 차단 정도 표현

UIViewBase
	모든 View의 Show/Hide 공통화

UIManager
	Screen / Overlay / HUD / 입력 정책 / TimeScale 관리
```

이 구조를 기준으로 하면 6월 17일 플레이 가능한 빌드에도 대응 가능하고, 이후 정식 구현에서도 크게 뜯어고칠 가능성이 낮다.
