using UnityEngine;
using UnityEngine.UI;

// 일시정지 메뉴 전체 현황 페이지
// 플레이어 현재 상태를 화면에 표시만 해주는 스크립트
public class PauseStatusPageView : MonoBehaviour
{
    [Header("Level")]
    // 현재 플레이어 레벨을 표시하는 텍스트
    [SerializeField] private Text levelText;

    [Header("EXP")]
    // 현재 경험치 비율을 채워서 보여주는 게이지 이미지
    [SerializeField] private Image expFillImage;
    // 현재 경험치와 다음 레벨까지 필요한 경험치를 숫자로 표시하는 텍스트
    [SerializeField] private Text expText;

    [Header("HP")]
    // 현재 HP 비율을 채워서 보여주는 게이지 이미지
    [SerializeField] private Image hpFillImage;
    // 현재 HP와 최대 HP를 숫자로 표시하는 텍스트
    [SerializeField] private Text hpText;

    [Header("Life")]
    // 플레이어의 남은 목숨을 아이콘으로 표시하기 위한 배열
    [SerializeField] private Image[] lifeIcons;

    [Header("Active Skills")]
    // 현재 장착 중인 액티브 스킬 슬롯들을 표시하는 View 배열
    [SerializeField] private PauseSkillInfoView[] activeSkillViews;

    [Header("Passive Skills")]
    // 보유 패시브 스킬을 표시할 고정 View 배열
    [SerializeField] private PauseSkillInfoView[] passiveSkillViews;
    // 보유 패시브 스킬이 없을 때 표시할 빈 상태 안내 오브젝트
    [SerializeField] private GameObject emptyPassiveSkillObject;

    [Header("Character Preview")]
    // 캐릭터 모델 또는 프리뷰 표시 영역의 루트 오브젝트
    [SerializeField] private GameObject characterPreviewRoot;
    // 캐릭터 프리뷰가 없거나 아직 구현되지 않았을 때 사용할 대체 이미지
    [SerializeField] private Image fallbackCharacterImage;

    // 마지막으로 전달받은 플레이어 레벨
    private int currentLevel = 1;
    // 마지막으로 전달받은 현재 경험치
    private float currentExp;
    // 마지막으로 전달받은 다음 레벨까지 필요한 경험치
    private float requiredExp = 1f;
    // 마지막으로 전달받은 현재 HP
    private float currentHp = 1f;
    // 마지막으로 전달받은 최대 HP
    private float maxHp = 1f;
    // 마지막으로 전달받은 현재 목숨 개수
    private int currentLife;
    // 마지막으로 전달받은 최대 목숨 개수
    private int maxLife;
    // 마지막으로 전달받은 액티브 스킬 표시 데이터
    private UIPauseSkillInfoData[] currentActiveSkills;
    // 마지막으로 전달받은 패시브 스킬 표시 데이터
    private UIPauseSkillInfoData[] currentPassiveSkills;

    // 이 페이지가 생성될 때 호출되는 초기화 지점
    // 이후 구현 단계에서는 이곳에서 EventBus 구독을 시작할 예정
    private void Awake()
    {
        SubscribeEvents();
    }

    // 페이지 오브젝트가 다시 활성화될 때 호출됩니다.
    // 이후 구현 단계에서는 캐싱된 상태값으로 화면을 다시 갱신하는 지점으로 사용합니다.
    private void OnEnable()
    {
        // 2026.08.10_초기 Pause 상태를 활성화 이후 다시 받아 이벤트 누락을 보완한다.
        EventBus<UIRequestPauseStatusEvent>.Publish(default);
        RefreshAll();
    }

    // 오브젝트가 파괴될 때 호출되는 정리 지점
    // EventBus는 static 구조이므로 구독을 시작했다면 이곳에서 반드시 해제해야 합니다.
    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    // 전체 현황 페이지가 받아야 할 EventBus 이벤트를 구독하는 메서드
    // 이후 구현 단계에서 UISetPauseStatusEvent와 UIResetEvent를 연결합니다.
    private void SubscribeEvents()
    {
        EventBus<UISetPauseStatusEvent>.action += HandleSetPauseStatus;
        EventBus<UIResetEvent>.action += HandleReset;
    }

    // SubscribeEvents()에서 연결한 EventBus 이벤트 구독을 해제하는 메서드
    // 오브젝트 파괴 후에도 EventBus가 이 View를 참조하지 않도록 막기 위해 필요합니다.
    private void UnsubscribeEvents()
    {
        EventBus<UISetPauseStatusEvent>.action -= HandleSetPauseStatus;
        EventBus<UIResetEvent>.action -= HandleReset;
    }

    // 외부 시스템에서 전달한 일시정지 전체 현황 스냅샷을 저장하고 화면을 갱신합니다.
    // 들어온 값은 안전한 범위로 보정한 뒤 내부 캐시 변수에 저장합니다.
    private void HandleSetPauseStatus(UISetPauseStatusEvent eventData)
    {
        currentLevel = Mathf.Max(1, eventData.Level);
        currentExp = Mathf.Max(0f, eventData.CurrentExp);
        requiredExp = Mathf.Max(1f, eventData.RequiredExp);
        maxHp = Mathf.Max(1f, eventData.MaxHp);
        currentHp = Mathf.Clamp(eventData.CurrentHp, 0f, maxHp);
        maxLife = Mathf.Max(0, eventData.MaxLife);
        currentLife = Mathf.Clamp(eventData.CurrentLife, 0, maxLife);
        currentActiveSkills = eventData.ActiveSkills;
        currentPassiveSkills = eventData.PassiveSkills;

        RefreshAll();
    }

    // 전체 UI Reset 이벤트를 받았을 때 전체 현황 페이지도 기본 상태로 되돌립니다.
    private void HandleReset(UIResetEvent eventData)
    {
        ResetStatus();
    }

    // 현재 페이지가 들고 있는 플레이어 상태 캐시를 기본값으로 초기화합니다.
    // 초기화 후에는 화면 표시도 함께 갱신합니다.
    private void ResetStatus()
    {
        currentLevel = 1;
        currentExp = 0f;
        requiredExp = 1f;
        currentHp = 1f;
        maxHp = 1f;
        currentLife = 0;
        maxLife = 0;
        currentActiveSkills = null;
        currentPassiveSkills = null;

        RefreshAll();
    }

    // 전체 현황 페이지에 포함된 모든 표시 요소를 현재 캐시값 기준으로 다시 그립니다.
    private void RefreshAll()
    {
        RefreshLevel();
        RefreshExp();
        RefreshHp();
        RefreshLife();
        RefreshActiveSkills();
        RefreshPassiveSkills();
        RefreshCharcterPreview();
    }

    // 플레이어 레벨 텍스트를 갱신합니다.
    private void RefreshLevel()
    {
        SetText(levelText, $"{currentLevel}Lv");
    }

    // 경험치 게이지와 경험치 수치 텍스트를 갱신합니다.
    private void RefreshExp()
    {
        float ratio = Mathf.Clamp01(currentExp / requiredExp);

        if (expFillImage != null)
        {
            expFillImage.fillAmount = ratio;
        }

        SetText(expText, $"{Mathf.FloorToInt(currentExp)} / {Mathf.FloorToInt(requiredExp)}");
    }

    // HP 게이지와 HP 수치 텍스트를 갱신합니다.
    private void RefreshHp()
    {
        float ratio = Mathf.Clamp01(currentHp / maxHp);

        if (hpFillImage != null)
        {
            hpFillImage.fillAmount = ratio;
        }

        SetText(hpText, $"{Mathf.CeilToInt(currentHp)} / {Mathf.CeilToInt(maxHp)}");
    }

    // 목숨 아이콘 표시를 갱신합니다.
    // 최대 목숨 개수까지만 아이콘을 보이고, 현재 남은 목숨이 아닌 아이콘은 흐리게 표시합니다.
    private void RefreshLife()
    {
        if (lifeIcons == null)
            return;

        for (int i = 0; i < lifeIcons.Length; i++)
        {
            if (lifeIcons[i] == null)
                continue;

            bool shouldShow = i < maxLife;
            bool isActiveLife = i < currentLife;

            lifeIcons[i].gameObject.SetActive(shouldShow);
            lifeIcons[i].color = isActiveLife ? Color.white : new Color(1f, 1f, 1f, 0.25f);
        }
    }


    // 현재 장착 중인 액티브 스킬 슬롯 표시를 갱신합니다.
    // 데이터가 없는 슬롯은 Clear()로 빈 상태로 되돌립니다.
    private void RefreshActiveSkills()
    {
        if (activeSkillViews == null)
            return;

        for (int i = 0; i < activeSkillViews.Length; i++)
        {
            if (activeSkillViews[i] == null)
                continue;

            bool hasData = currentActiveSkills != null && i < currentActiveSkills.Length;

            if (hasData)
            {
                activeSkillViews[i].gameObject.SetActive(true);
                activeSkillViews[i].Setup(currentActiveSkills[i]);
            }
            else
            {
                activeSkillViews[i].Clear();
            }
        }
    }

    // 보유 패시브 스킬 목록을 현재 데이터 기준으로 다시 생성합니다.
    // 기존에 생성된 목록은 먼저 제거하고, 새 데이터가 있을 때만 프리팹을 복제합니다.
    private void RefreshPassiveSkills()
    {
        bool hasPassiveSkills = currentPassiveSkills != null && currentPassiveSkills.Length > 0;

        if (emptyPassiveSkillObject != null)
        {
            emptyPassiveSkillObject.SetActive(!hasPassiveSkills);
        }

        if (passiveSkillViews == null)
            return;

        for (int i = 0; i < passiveSkillViews.Length; i++)
        {
            if (passiveSkillViews[i] == null)
                continue;

            bool hasData = currentPassiveSkills != null && i < currentPassiveSkills.Length;

            if (hasData)
            {
                passiveSkillViews[i].gameObject.SetActive(true);
                passiveSkillViews[i].Setup(currentPassiveSkills[i]);
            }
            else
            {
                passiveSkillViews[i].Clear();
                passiveSkillViews[i].gameObject.SetActive(false);
            }
        }
    }

    // 캐릭터 프리뷰 영역과 대체 이미지 표시 상태를 갱신합니다.
    // 현재는 실제 3D 프리뷰 로딩이 아니라 표시 영역을 준비하는 수준
    private void RefreshCharcterPreview()
    {
        if (characterPreviewRoot != null)
        {
            characterPreviewRoot.SetActive(true);
        }

        if (fallbackCharacterImage != null)
        {
            fallbackCharacterImage.enabled = fallbackCharacterImage.sprite != null;
        }
    }

    // Text 참조가 비어 있어도 오류가 나지 않게 처리하는 공통 텍스트 설정 메서드
    private void SetText(Text targetText, string value)
    {
        if (targetText == null)
            return;

        targetText.text = value;
    }
}
