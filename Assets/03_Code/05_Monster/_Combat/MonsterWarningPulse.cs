using UnityEngine;

[DisallowMultipleComponent]
public class MonsterWarningPulse : MonoBehaviour
{
    [Header("Pulse")]
    [SerializeField] private Transform pulseTarget;
    [SerializeField] private float pulseSpeed = 8f;
    [SerializeField] private float minScaleMultiplier = 0.85f;
    [SerializeField] private float maxScaleMultiplier = 1.15f;

    private Vector3 baseScale;
    private bool isInitialized;

    // 경고 연출 대상과 기준 크기를 준비합니다
    private void Awake()
    {
        InitializePulseTarget();
    }

    // 활성화될 때 현재 크기를 기준 크기로 다시 저장합니다
    private void OnEnable()
    {
        InitializePulseTarget();
        baseScale = pulseTarget.localScale;
    }

    // 활성화된 동안 경고 오브젝트의 크기를 반복해서 변화시킵니다
    private void Update()
    {
        if (!isInitialized || pulseTarget == null)
        {
            return;
        }

        float normalizedPulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
        float scaleMultiplier = Mathf.Lerp(
            minScaleMultiplier,
            maxScaleMultiplier,
            normalizedPulse
        );

        pulseTarget.localScale = baseScale * scaleMultiplier;
    }

    // 비활성화될 때 경고 오브젝트를 기준 크기로 되돌립니다
    private void OnDisable()
    {
        if (!isInitialized || pulseTarget == null)
        {
            return;
        }

        pulseTarget.localScale = baseScale;
    }

    // 인스펙터 값을 안전한 범위로 보정합니다
    private void OnValidate()
    {
        pulseSpeed = Mathf.Max(0f, pulseSpeed);
        minScaleMultiplier = Mathf.Max(0f, minScaleMultiplier);
        maxScaleMultiplier = Mathf.Max(minScaleMultiplier, maxScaleMultiplier);

        if (pulseTarget == null)
        {
            pulseTarget = transform;
        }
    }

    // 경고 연출 대상과 기준 크기를 저장합니다
    private void InitializePulseTarget()
    {
        if (pulseTarget == null)
        {
            pulseTarget = transform;
        }

        baseScale = pulseTarget.localScale;
        isInitialized = true;
    }
}
