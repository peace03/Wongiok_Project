// 초기화 인터페이스
public interface IInitializable
{
    // 중요도
    public int Priority { get; }

    // 초기화 함수
    public void Init();
}

// 데미지
public interface IDamageable
{
    public void TakeDamage(float amount);
}