using UnityEngine;

[DisallowMultipleComponent]
public class MonsterAnimatorDriver : MonoBehaviour
{
    [Header("Animator")]
    [SerializeField] private Animator animator;
    [SerializeField] private string movingBoolName = "IsMoving";
    [SerializeField] private float movingSpeedThreshold = 0.05f;

    private int movingBoolHash;
    private Vector3 previousPosition;

    // Animator와 이동 파라미터를 준비합니다
    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        movingBoolHash = Animator.StringToHash(movingBoolName);
        previousPosition = transform.position;
    }

    // 활성화될 때 위치와 이동 상태를 초기화합니다
    private void OnEnable()
    {
        previousPosition = transform.position;
        SetMoving(false);
    }

    // 수평 이동 속도를 기준으로 이동 애니메이션 상태를 갱신합니다
    private void Update()
    {
        float deltaTime = Time.deltaTime;

        if (deltaTime <= 0f)
        {
            previousPosition = transform.position;
            return;
        }

        float horizontalDistance = Mathf.Abs(
            transform.position.x - previousPosition.x
        );

        float horizontalSpeed = horizontalDistance / deltaTime;
        bool isMoving = horizontalSpeed > movingSpeedThreshold;

        SetMoving(isMoving);
        previousPosition = transform.position;
    }

    // 인스펙터 값을 유효한 범위로 보정합니다
    private void OnValidate()
    {
        movingSpeedThreshold = Mathf.Max(0f, movingSpeedThreshold);
    }

    // Animator의 이동 여부 파라미터를 설정합니다
    private void SetMoving(bool isMoving)
    {
        if (animator == null || string.IsNullOrEmpty(movingBoolName))
        {
            return;
        }

        animator.SetBool(movingBoolHash, isMoving);
    }
}