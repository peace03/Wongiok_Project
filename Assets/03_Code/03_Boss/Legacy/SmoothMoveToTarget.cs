using UnityEngine;

public class SmoothMoveToTarget : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float arriveTime = 0.3f;
    private Vector3 velocity = Vector3.zero;

    private void LateUpdate()
    {
        if (target == null) return;
        //목표 위치
        Vector3 targetPos = target.position;
        //목표 위치로 부드럽게 이동
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, arriveTime);
    }
}
