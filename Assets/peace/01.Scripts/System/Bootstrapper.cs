using UnityEngine;
using System.Linq;

//각자 스크립트 초기화 순서 정해줄 때 사용
public enum InitOrder
{
    Player = 0,
    Skill = 100,
    Mob = 200,
    Boss = 300,
    UI = 400
}

public class Bootstrapper : MonoBehaviour
{
    private void Awake()
    {
        //인스펙터로 참조하지 말고 전체적으로 1회 찾아오기
        var managers = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
            .OfType<IInitializable>()
            .OrderBy(m => m.Priority)
            .ToArray();

        //Priority로 정렬된 managers순서대로 초기화 및 ServiceLocator 등록
        foreach(var manager in managers)
        {
            manager.Init();
            ServiceLocator.Register(manager.GetType(), manager);
            //GetType()은 IInitializable같은 껍데기 타입이 아닌 알맹이 진짜 타입을 반환함
        }
    }
}
