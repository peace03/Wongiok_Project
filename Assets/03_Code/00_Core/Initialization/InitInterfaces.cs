//BootStrapper에서 초기화 하려는 스크립트용 인터페이스
public interface IInitializable
{
    public int Priority { get; }    //초기화 순서
    public void Init();
}

// 데미지
public interface IDamageable
{
    bool CanTakeDamage => true;

    public void TakeDamage(float amount);
}
