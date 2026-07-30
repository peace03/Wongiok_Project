using UnityEngine;

public sealed class MonsterFlyingAnimationEventRelay : MonoBehaviour
{
    [SerializeField]
    private MonsterFlyingSelfDestructAI owner;

    // 애니메이션 이벤트를 전달할 공중 자폭 AI를 찾습니다
    private void Awake()
    {
        if (owner == null)
        {
            owner =
                GetComponentInParent<
                    MonsterFlyingSelfDestructAI
                >();
        }
    }

    // 폭발 애니메이션 이벤트를 공중 자폭 AI에 전달합니다
    public void ExecuteExplosionImpact()
    {
        Debug.Log(
            $"{name}: 폭발 Animation Event 수신",
            this
        );

        if (owner == null)
        {
            Debug.LogError(
                $"{name}: 공중 자폭 AI 참조가 없습니다.",
                this
            );

            return;
        }

        owner.ExecuteExplosionImpact();
    }
}