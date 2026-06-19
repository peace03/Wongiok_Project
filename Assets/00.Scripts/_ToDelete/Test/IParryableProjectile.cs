using UnityEngine;

// 플레이어 패리가 처리할 수 있는 몬스터 투사체용 공통 인터페이스입니다.
// 실제 몬스터 투사체 구현은 이 인터페이스만 맞추면 PlayerParry와 연결됩니다.
public interface IParryableProjectile
{
    // 패리 범위 안에서 가장 가까운 투사체를 고를 때 사용하는 위치입니다.
    Vector3 Position { get; }

    // 패리가 성공했을 때 투사체가 스스로 제거되거나 반응하도록 호출됩니다.
    bool TryParry(GameObject parryOwner);

    // 발사자 자신이나 그 자식 오브젝트를 패리 대상으로 보지 않기 위한 검사입니다.
    bool IsOwnedBy(GameObject candidate);

    // 패리 가능 범위 안에 들어왔을 때 투사체가 시각 표시를 켜고 끄는 진입점입니다.
    void SetParryReadyVisual(bool isReady);
}
