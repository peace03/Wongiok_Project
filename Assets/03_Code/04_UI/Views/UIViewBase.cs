using UnityEngine;

// 모든 UI View가 공통으로 상속받는 부모 클래스입니다.
// 화면을 직접 그리거나 데이터를 계산하지 않고, Show/Hide 같은 기본 표시 동작만 담당합니다.
//
// CanvasGroup은 UI 요소의 투명도, 클릭 가능 여부, Raycast 차단 여부를 한 번에 제어하는 컴포넌트입니다.
// 이 클래스는 CanvasGroup을 반드시 필요로 하므로, 컴포넌트가 없으면 Unity가 자동으로 추가합니다.
[RequireComponent(typeof(CanvasGroup))]
public abstract class UIViewBase : MonoBehaviour
{
    // true면 Hide()가 호출된 뒤 GameObject까지 비활성화합니다.
    // false면 GameObject는 켜둔 채 CanvasGroup만 숨기므로, 애니메이션이나 레이아웃 유지가 필요할 때 사용할 수 있습니다.
    [SerializeField] private bool deactivateOnHide = true;

    // 이 View의 표시, 입력, Raycast 상태를 제어하는 CanvasGroup입니다.
    private CanvasGroup canvasGroup;

    // 현재 View가 UIManager 기준으로 보이는 상태인지 나타냅니다.
    // GameObject.activeSelf와 완전히 같은 의미는 아니므로, 외부에서는 이 값을 표시 상태 기준으로 사용합니다.
    public bool IsVisible { get; private set; }

    // View가 처음 생성될 때 필요한 컴포넌트 참조를 캐싱합니다.
    // 자식 View가 Awake를 override할 경우 base.Awake()를 호출해야 CanvasGroup 제어가 정상 동작합니다.
    protected virtual void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    // View를 표시합니다.
    // 이미 보이는 상태라면 중복 호출을 막기 위해 바로 종료합니다.
    public void Show()
    {
        if (IsVisible)
            return;

        // Hide()에서 GameObject를 꺼둔 경우 다시 켜야 CanvasGroup 설정과 OnShow가 의미를 가집니다.
        gameObject.SetActive(true);

        IsVisible = true;
        SetCanvasState(1f, true, true);

        // 실제 View별 표시 갱신은 자식 클래스가 OnShow에서 확장합니다.
        OnShow();
    }

    // View를 숨깁니다.
    // 이미 숨겨진 상태라면 중복 호출을 막기 위해 바로 종료합니다.
    public void Hide()
    {
        if (!IsVisible)
            return;

        IsVisible = false;
        SetCanvasState(0f, false, false);

        // 실제 View별 정리 작업은 자식 클래스가 OnHide에서 확장합니다.
        OnHide();

        if (deactivateOnHide)
        {
            gameObject.SetActive(false);
        }
    }

    // View의 상호작용 가능 여부를 설정합니다.
    // 버튼 클릭 가능 여부와 Raycast 차단 여부를 같이 바꿔서, 비활성 UI가 입력을 가로채지 않게 합니다.
    public void SetInteractable(bool isInteractable)
    {
        canvasGroup.interactable = isInteractable;
        canvasGroup.blocksRaycasts = isInteractable;
    }

    // View를 흐리게 보여줄지 설정합니다.
    // 예: Cutscene이나 LevelUp Overlay가 떠 있을 때 Player HUD를 45% 투명도로 낮추는 용도입니다.
    public void SetDimmed(bool isDimmed)
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = isDimmed ? 0.45f : 1f;
    }

    // CanvasGroup의 핵심 상태를 한 번에 적용합니다.
    // 외부에서 세부 값을 직접 만지지 않도록 private으로 숨겨 둡니다.
    private void SetCanvasState(float alpha, bool isInteractable, bool blocksRaycasts)
    {
        canvasGroup.alpha = alpha;
        canvasGroup.interactable = isInteractable;
        canvasGroup.blocksRaycasts = blocksRaycasts;
    }

    // View가 표시될 때 자식 클래스에서 필요한 갱신을 넣는 확장 지점입니다.
    // 예: 텍스트 갱신, 버튼 상태 초기화, 선택 커서 초기화
    protected virtual void OnShow()
    {
    }

    // View가 숨겨질 때 자식 클래스에서 필요한 정리를 넣는 확장 지점입니다.
    // 예: 임시 선택 상태 초기화, 진행 중인 연출 정리
    protected virtual void OnHide()
    {
    }
}
