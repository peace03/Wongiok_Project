using System;
using UnityEngine;

// T에 값 타입만 들어오도록 제한한 공통 이벤트 버스입니다.
// 이유: 이벤트 데이터 자체를 struct로 제한해 불필요한 힙 할당과 GC 발생을 줄이기 위한 의도입니다.
public static class EventBus<T> where T : struct
{
    // generic 타입별로 별도의 static 이벤트 저장소가 만들어집니다.
    // 예: EventBus<UIChangeScreenEvent>와 EventBus<UIResetEvent>는 서로 다른 구독 목록을 가집니다.
    public static event Action<T> action;

    // 이벤트를 발행하고, 구독 중인 모든 핸들러에 구조체 이벤트 데이터를 전달합니다.
    public static void Publish(T eventData) => action?.Invoke(eventData);

}

// 구조체 모음 스크립트======================================================
public struct Onhpchange { };
public struct OnHit //생성자로 변수값 설정해준다.
{
    public Transform hitPos { get; private set; }
    public float attackPower { get; private set; }

    public OnHit(Transform hitPos, float attackPower)
    {
        this.hitPos = hitPos;
        this.attackPower = attackPower;
    }
}

// 발행자 스크립트, 사용 예시===============================================
public class Test : MonoBehaviour
{
    [SerializeField] private Transform showEffect;
    private float amount;

    public void Method()
    {
        // 구조체 안에 아무것도 없거나 값을 넣고 싶지 않을 때 default를 사용합니다.
        // default는 new 구조체()와 같은 역할을 하며, 변수가 있다면 기본값으로 채웁니다.
        EventBus<Onhpchange>.Publish(default);

        // 구조체 값을 이벤트와 함께 전달하고 싶을 경우 생성자로 데이터를 담아 발행합니다.
        OnHit hit = new OnHit(showEffect, amount);
        EventBus<OnHit>.Publish(hit);
    }
}
