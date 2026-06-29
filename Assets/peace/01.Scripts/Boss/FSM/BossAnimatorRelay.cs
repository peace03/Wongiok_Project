using UnityEngine;

//SpinShard용이었음
//OnAnimatorMove()는 Animator가 붙어있는 오브젝트 내에서 실행을 해줘야하기 때문에
//해당 메서드에서 돌려야하는 메서드를 대신 호출해준다.
public class BossAnimatorRelay : MonoBehaviour, IInitializable
{
    public int Priority => (int)InitOrder.Boss + 5;
    private Cinderella_Patterns logics;

    public void Init()
    {
        logics = ServiceLocator_Y.Get<Cinderella_Patterns>();
    }

    //매 프레임 루트모션 통제권을 넘겨줌
    private void OnAnimatorMove()
    {
        if (logics != null) logics.OnAnimatorMoveCallback();
    }
}
