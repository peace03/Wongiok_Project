using System.Collections;
using UnityEngine;

// 전용 피격 이벤트를 받아 피격된 오브젝트의 색을 짧게 붉게 바꾸는 시각 피드백입니다.
// 플레이어와 몬스터 모두 같은 컴포넌트를 사용하도록 분리했습니다.
public class HitFlashFeedback : MonoBehaviour
{
    // 피격 순간에 적용할 색입니다.
    [SerializeField] private Color hitColor = new Color(1f, 0.35f, 0.35f, 1f);

    // 피격 색이 유지되는 시간입니다.
    [SerializeField] private float flashDuration = 0.12f;

    // 자식까지 포함해 색을 바꿀 Renderer 목록입니다.
    private Renderer[] targetRenderers;

    // material은 Renderer별로 여러 개일 수 있어 2차원 배열로 보관합니다.
    private Material[][] targetMaterials;

    // 피격 색을 되돌리기 위한 원래 색입니다.
    private Color[][] originalColors;

    // URP는 _BaseColor, 기본 셰이더는 _Color를 쓰므로 머티리얼별 색상 프로퍼티를 저장합니다.
    private string[][] colorProperties;

    // 피격이 연속으로 들어올 때 기존 코루틴을 멈추고 새로 시작하기 위한 핸들입니다.
    private Coroutine flashRoutine;

    private void Awake()
    {
        CacheRenderers();
    }

    private void OnEnable()
    {
        // 컴포넌트가 런타임에 추가된 경우 Awake보다 OnEnable 흐름이 먼저 필요할 수 있어 보정합니다.
        if (targetRenderers == null)
            CacheRenderers();

        // 실제 데미지가 적용된 뒤에만 색을 바꿉니다.
        EventBus<PlayerDamagedEvent>.action += OnPlayerDamaged;
        EventBus<MonsterDamagedEvent>.action += OnMonsterDamaged;
    }

    private void OnDisable()
    {
        EventBus<PlayerDamagedEvent>.action -= OnPlayerDamaged;
        EventBus<MonsterDamagedEvent>.action -= OnMonsterDamaged;

        // 비활성화될 때 피격 색이 남아 있지 않도록 정리합니다.
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
        }

        RestoreOriginalColors();
    }

    private void OnPlayerDamaged(PlayerDamagedEvent eventData)
    {
        // 다른 플레이어 오브젝트가 맞은 이벤트에는 반응하지 않습니다.
        if (eventData.PlayerObject != gameObject) return;

        Flash();
    }

    private void OnMonsterDamaged(MonsterDamagedEvent eventData)
    {
        // 다른 몬스터 오브젝트가 맞은 이벤트에는 반응하지 않습니다.
        if (eventData.MonsterObject != gameObject) return;

        Flash();
    }

    public void Flash()
    {
        // 렌더러가 늦게 붙었거나 자식 오브젝트가 바뀐 경우를 위해 한 번 더 캐싱합니다.
        if (targetRenderers == null || targetRenderers.Length == 0)
            CacheRenderers();

        if (targetRenderers.Length == 0) return;

        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        // 색을 바꾼 뒤 짧게 기다렸다가 원래 색으로 되돌립니다.
        SetColor(hitColor);
        yield return new WaitForSeconds(flashDuration);
        RestoreOriginalColors();
        flashRoutine = null;
    }

    private void CacheRenderers()
    {
        // sharedMaterials가 아닌 materials를 사용해 인스턴스 머티리얼만 바꾸도록 합니다.
        targetRenderers = GetComponentsInChildren<Renderer>();
        targetMaterials = new Material[targetRenderers.Length][];
        originalColors = new Color[targetRenderers.Length][];
        colorProperties = new string[targetRenderers.Length][];

        for (int i = 0; i < targetRenderers.Length; i++)
        {
            targetMaterials[i] = targetRenderers[i].materials;
            originalColors[i] = new Color[targetMaterials[i].Length];
            colorProperties[i] = new string[targetMaterials[i].Length];

            for (int j = 0; j < targetMaterials[i].Length; j++)
            {
                colorProperties[i][j] = GetColorProperty(targetMaterials[i][j]);

                if (string.IsNullOrEmpty(colorProperties[i][j]))
                    continue;

                originalColors[i][j] = targetMaterials[i][j].GetColor(colorProperties[i][j]);
            }
        }
    }

    private void SetColor(Color color)
    {
        for (int i = 0; i < targetMaterials.Length; i++)
        {
            for (int j = 0; j < targetMaterials[i].Length; j++)
            {
                if (string.IsNullOrEmpty(colorProperties[i][j]))
                    continue;

                targetMaterials[i][j].SetColor(colorProperties[i][j], color);
            }
        }
    }

    private void RestoreOriginalColors()
    {
        if (targetMaterials == null || originalColors == null || colorProperties == null)
            return;

        for (int i = 0; i < targetMaterials.Length; i++)
        {
            for (int j = 0; j < targetMaterials[i].Length; j++)
            {
                if (string.IsNullOrEmpty(colorProperties[i][j]))
                    continue;

                targetMaterials[i][j].SetColor(colorProperties[i][j], originalColors[i][j]);
            }
        }
    }

    private string GetColorProperty(Material material)
    {
        // 사용 중인 셰이더에 맞는 색상 프로퍼티를 찾습니다.
        if (material == null) return string.Empty;
        if (material.HasProperty("_BaseColor")) return "_BaseColor";
        if (material.HasProperty("_Color")) return "_Color";

        return string.Empty;
    }
}
