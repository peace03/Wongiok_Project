using System.Collections;
using UnityEngine;

public class SideViewCameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 2f, -10f);
    [SerializeField] private float followSpeed = 10f;

    private Transform previousTarget;
    private Coroutine transitionRoutine;
    private bool isLocked;
    private bool isTransitioning;

    // 카메라가 추적 대상 또는 고정 지점을 따라가도록 처리합니다
    private void LateUpdate()
    {
        if (isTransitioning || target == null)
        {
            return;
        }

        Vector3 desiredPosition =
            isLocked
                ? target.position
                : target.position + offset;

        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            followSpeed * Time.deltaTime
        );
    }

    // 외부에서 카메라 추적 대상을 설정합니다
    public void SetTarget(Transform newTarget)
    {
        if (isLocked)
        {
            previousTarget = newTarget;
            return;
        }

        target = newTarget;
        previousTarget = newTarget;
    }

    // 지정된 시간 동안 카메라를 고정 지점으로 이동시킵니다
    public Coroutine MoveAndLockTo(
        Transform lockTarget,
        float transitionDuration)
    {
        if (lockTarget == null)
        {
            return null;
        }

        StopTransition();

        if (!isLocked)
        {
            previousTarget = target;
        }

        target = lockTarget;
        isLocked = true;

        if (transitionDuration <= 0f)
        {
            transform.position = lockTarget.position;
            return null;
        }

        transitionRoutine = StartCoroutine(
            MoveToLockPointRoutine(
                lockTarget,
                transitionDuration
            )
        );

        return transitionRoutine;
    }

    // 카메라가 기존 방식으로 고정 지점을 따라가도록 설정합니다
    public void LockTo(Transform lockTarget)
    {
        if (lockTarget == null)
        {
            return;
        }

        StopTransition();

        if (!isLocked)
        {
            previousTarget = target;
        }

        target = lockTarget;
        isLocked = true;
    }

    // 카메라 고정을 해제하고 이전 추적 대상으로 되돌립니다
    public void Unlock()
    {
        StopTransition();

        isLocked = false;

        if (previousTarget != null)
        {
            target = previousTarget;
        }
    }

    // 카메라를 시작 위치에서 고정 지점까지 부드럽게 이동시킵니다
    private IEnumerator MoveToLockPointRoutine(
        Transform lockTarget,
        float transitionDuration)
    {
        isTransitioning = true;

        Vector3 startPosition = transform.position;
        float elapsedTime = 0f;

        while (elapsedTime < transitionDuration)
        {
            if (lockTarget == null)
            {
                isLocked = false;
                target = previousTarget;
                isTransitioning = false;
                transitionRoutine = null;
                yield break;
            }

            elapsedTime += Time.deltaTime;

            float progress = Mathf.Clamp01(
                elapsedTime / transitionDuration
            );

            float smoothProgress = Mathf.SmoothStep(
                0f,
                1f,
                progress
            );

            transform.position = Vector3.LerpUnclamped(
                startPosition,
                lockTarget.position,
                smoothProgress
            );

            yield return null;
        }

        if (lockTarget != null)
        {
            transform.position = lockTarget.position;
        }

        isTransitioning = false;
        transitionRoutine = null;
    }

    // 진행 중인 카메라 이동을 중단합니다
    private void StopTransition()
    {
        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
        }

        transitionRoutine = null;
        isTransitioning = false;
    }
}