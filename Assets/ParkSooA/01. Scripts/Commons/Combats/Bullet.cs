using UnityEngine;
using UnityEngine.Pool;
using System;
using System.Collections;

public class Bullet : MonoBehaviour, IPoolable
{
    [Header("총알 스탯")][Space(10)]
    [Header("소유자 레이어")]
    [SerializeField] private LayerMask ownerLayer;          // 소유자 레이어
    [Header("데미지")]
    [SerializeField] private float damage = 0f;             // 데미지
    [Header("관통 횟수")]
    [SerializeField] private int penetrationCount = 1;      // 관통 횟수
    [Header("속도")]
    [SerializeField] private float speed = 10f;             // 속도
    [Header("지속 시간")]
    [SerializeField] private float duration = 5f;           // 지속 시간

    private IObjectPool<GameObject> returnRef;              // 반납 오브젝트 풀 주소

    private Coroutine returnCoroutine;                      // 반납 코루틴
    private WaitForSeconds waitTime;                        // 대기 시간

    private bool startFire = false;                         // 사격 시작 여부

    // 총알 발사
    private void Update()
    {
        // 사격이 시작되지 않았다면
        if (!startFire)
            return;

        transform.position += speed * Time.deltaTime * transform.forward;
    }

    private void OnTriggerEnter(Collider other)
    {
        // 닿은 물체의 레이어가 소유자 레이어와 같다면
        if (((1 << other.gameObject.layer) & ownerLayer.value) != 0)
            return;

        // 데미지를 입을 수 있는 물체라면
        if (other.TryGetComponent<IDamageable>(out var target))
        {
            // 데미지 전달
            target.TakeDamage(damage);
            // 관통 횟수 감소
            penetrationCount = Math.Max(0, penetrationCount - 1);
        }

        // 관통 횟수가 남아있지 않다면
        if (penetrationCount <= 0)
            // 총알 반납
            returnRef.Release(gameObject);
    }

    private void OnDisable()
    {
        // 반납 코루틴이 비어있지 않다면
        if(returnCoroutine != null)
        {
            // 반납 중지
            StopCoroutine(returnCoroutine);
            // 반납 코루틴 초기화
            returnCoroutine = null;
        }
    }

    // 오브젝트 풀 주소 설정 함수(반환 주소 설정)
    public void SetPoolRef(IObjectPool<GameObject> poolRef) => returnRef = poolRef;

    // 사격 시작 함수
    public void StartFire(Transform origin, LayerMask ownerLayer, float damage, int penetrationCount = 1)
    {
        // 반납 코루틴이 비어있지 않다면
        if (returnCoroutine != null)
            // 반납 중지
            StopCoroutine(returnCoroutine);

        // 위치, 각도 설정
        transform.SetPositionAndRotation(origin.position, origin.rotation);
        // 총알 정보 설정
        SetInfo(ownerLayer, damage, penetrationCount);
        // 사격 시작
        startFire = true;
        // 대기 시간 구하기
        waitTime = new WaitForSeconds(duration);
        // 반납 시작
        returnCoroutine = StartCoroutine(ReturnRoutine());
    }

    // 총알 정보 설정 함수
    private void SetInfo(LayerMask layer, float amount, int count)
    {
        // 소유자 레이어 설정
        ownerLayer = layer;
        // 데미지 설정
        damage = amount;
        // 관통 횟수 설정
        penetrationCount = count;
    }

    // 반납 코루틴 함수
    private IEnumerator ReturnRoutine()
    {
        // 대기 시간 기다리기
        yield return waitTime;
        // 사격 종료
        startFire = false;
        // 반납 코루틴 초기화
        returnCoroutine = null;
        // 총알 반납
        returnRef.Release(gameObject);
    }
}