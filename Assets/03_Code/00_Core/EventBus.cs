using System; //Action사용
using UnityEngine;

//T에 값타입만 들어오도록 제한
//이유: 참조타입이 들어오면 힙메모리에 저장되어 GC 유발
public static class EventBus<T> where T : struct
{
    //static 정적 영역에 저장되지만 generic 덕분에 동적으로 정적 영역에 생성함
    public static event Action<T> action;
    //발행자 이벤트 발생과 함께 구조체 데이터 전달시킴
    public static void Publish(T eventData) => action?.Invoke(eventData);
}

////구조체 모음 스크립트======================================================
//public struct Onhpchange { };
//public struct OnHit //생성자로 변수값 설정해준다.
//{
//    public Transform hitPos { get; private set; }
//    public float attackPower { get; private set; }
//
//    public OnHit(Transform hitPos, float attackPower)
//    {
//        this.hitPos = hitPos;
//        this.attackPower = attackPower;
//    }
//}
//
////발행자 스크립트, 사용 예시===============================================
//public class Test : MonoBehaviour
//{
//    [SerializeField] private Transform showEffect;
//    private float amount;
//
//    public void Method()
//    {
//        //구조체 안에 아무것도 없거나 값을 넣고싶지 않을 때 default사용
//        //default: new 구조체()와 같은 역할을 함. 변수가 있다면 기본값 저장
//        EventBus<Onhpchange>.Publish(default);
//
//        //구조체 값을 이벤트와 함께 전달하고 싶을 경우
//        OnHit hit = new OnHit(showEffect, amount);
//        EventBus<OnHit>.Publish(hit);
//    }
//}