using UnityEngine;

public class MonsterFacing : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private Transform visualPivot;
    [SerializeField] private bool invertVisualFacing;

    private bool facingRight = true;
    private Quaternion initialVisualLocalRotation;

    public bool FacingRight
    {
        get { return facingRight; }
    }

    // 시각 피벗의 초기 회전을 저장하고 현재 방향을 적용합니다
    private void Awake()
    {
        if (visualPivot == null)
        {
            Debug.LogWarning(
                $"{name}: MonsterFacing의 Visual Pivot이 지정되지 않았습니다.",
                this
            );

            return;
        }

        initialVisualLocalRotation =
            visualPivot.localRotation;

        ApplyVisualFacing();
    }

    // 대상 위치를 기준으로 바라보는 방향을 갱신합니다
    public void FaceTarget(Transform target)
    {
        if (target == null)
        {
            return;
        }

        if (target.position.x > transform.position.x)
        {
            SetFacingDirection(true);
        }
        else if (target.position.x < transform.position.x)
        {
            SetFacingDirection(false);
        }
    }

    // 몬스터의 논리 방향을 설정하고 시각 방향을 갱신합니다
    public void SetFacingDirection(bool lookRight)
    {
        facingRight = lookRight;
        ApplyVisualFacing();
    }

    // 현재 논리 방향을 시각 피벗의 Y축 회전으로 적용합니다
    private void ApplyVisualFacing()
    {
        if (visualPivot == null)
        {
            return;
        }

        bool visualFacingRight =
            invertVisualFacing
                ? !facingRight
                : facingRight;

        Quaternion facingRotation =
            visualFacingRight
                ? Quaternion.identity
                : Quaternion.Euler(0f, 180f, 0f);

        visualPivot.localRotation =
            facingRotation *
            initialVisualLocalRotation;
    }

    // 현재 바라보는 방향을 월드 방향으로 반환합니다
    public Vector3 GetFacingDirectionVector()
    {
        return facingRight
            ? Vector3.right
            : Vector3.left;
    }

    // 현재 바라보는 방향에 맞춰 오프셋을 계산합니다
    public Vector3 GetFacingOffset(Vector3 offset)
    {
        float direction =
            facingRight
                ? 1f
                : -1f;

        return new Vector3(
            offset.x * direction,
            offset.y,
            offset.z
        );
    }
}