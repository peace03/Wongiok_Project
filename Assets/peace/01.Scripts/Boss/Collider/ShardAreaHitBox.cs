using UnityEngine;
using static UnityEditor.LightingExplorerTableColumn;

public class ShardAreaHitBox : MonoBehaviour
{
    [Tooltip("유리 파편 지속시간")]
    [SerializeField] private float durationTime;
    [Tooltip("유리 파편 데미지 쿨타임")]
    [SerializeField] private float damageCoolTime;
    [Tooltip("공격력")]
    [SerializeField] private float damageAmount;

    private BoxCollider box;
    private float curTime;              //지속시간 계산
    private float damageTime;           //데미지 쿨 계산
    private bool isTriggered = false;   //공격 했는지 여부

    private void OnEnable()
    {
        box = GetComponent<BoxCollider>();
        EventBus<OnShardHitBox>.action += SpawnShardbox;
    }
    private void OnDisable()
    {
        EventBus<OnShardHitBox>.action -= SpawnShardbox;
    }

    //파편 지속데미지 박스 소환
    public void SpawnShardbox(OnShardHitBox data)
    {
        curTime = 0f;
        damageTime = 0f;
        isTriggered = false;

        transform.position = new Vector3(data.pos.position.x, 0, 0);
        box.enabled = true;
        Debug.Log("콜라이더 켜짐!");
    }

    private void Update()
    {
        if(box.enabled == true)
        {
            //파편 지속데미지 켜져있는 시간
            curTime += Time.deltaTime;
            if (curTime > durationTime)
            {
                box.enabled = false;
                curTime = 0f;
                Debug.Log("콜라이더 꺼짐!");
                return; //아래 계산 생략
            }
            //데미지 쿨타임 체크
            if (isTriggered) damageTime += Time.deltaTime;
            if (damageTime > damageCoolTime) //쿨타임 종료시 공격 가능
            {
                isTriggered = false;
                damageTime = 0f;
            }
        }
    }

    public void OnTriggerStay(Collider other)
    {
        if(other.CompareTag("Player") && !isTriggered)
        {
            isTriggered = true;
            other.GetComponent<PlayerStatus_Y>().
                TakeDamage(damageAmount);
        }
    }

    private void OnDrawGizmos()
    {
        if (box != null && box.enabled)
        {
            Gizmos.color = new Color(0.5f, 0f, 0f, 0.3f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
        }
    }
}
