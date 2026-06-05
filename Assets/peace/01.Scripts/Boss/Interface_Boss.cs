//BossController(중개자)를 통해 State들에게 연결될 확장성 인터페이스
using UnityEngine;

public interface IBossLogics
{
    public void Spawn();
    public void ExcuteIdleMove();
}