// Bootstrapper에서 초기화하려는 매니저 스크립트가 구현해야 하는 인터페이스입니다.
// 이 인터페이스를 구현한 MonoBehaviour는 Bootstrapper가 찾아서 Priority 순서대로 Init()을 호출합니다.
public interface IInitializable
{
    public int Priority { get; }    // 초기화 순서입니다. 숫자가 낮을수록 먼저 초기화됩니다.

    // 해당 매니저의 초기화 진입점입니다.
    // EventBus 구독, 내부 캐시 구성, 초기 상태 설정 등은 이 메서드에서 처리하는 방향으로 통일합니다.
    public void Init();
}
