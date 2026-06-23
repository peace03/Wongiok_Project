using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(PlayerStatus))]
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerAttack))]
[RequireComponent(typeof(PlayerParry))]
[RequireComponent(typeof(PlayerCheckpointTracker))]
[RequireComponent(typeof(PlayerHealItemInventory))]
[RequireComponent(typeof(PlayerLifeTracker))]
[RequireComponent(typeof(PlayerExperienceTracker))]
[RequireComponent(typeof(HitFlashFeedback))]
public class PlayerInitializer : MonoBehaviour, IInitializable
{
    public int Priority => (int)InitOrder.Player;

    public PlayerController Controller { get; private set; }
    public PlayerStatus Status { get; private set; }
    public PlayerMovement Movement { get; private set; }
    public PlayerAttack Attack { get; private set; }
    public PlayerParry Parry { get; private set; }
    public PlayerCheckpointTracker CheckpointTracker { get; private set; }
    public PlayerHealItemInventory HealItemInventory { get; private set; }
    public PlayerLifeTracker LifeTracker { get; private set; }
    public PlayerExperienceTracker ExperienceTracker { get; private set; }
    public HitFlashFeedback HitFeedback { get; private set; }

    public void Init()
    {
        // 플레이어 루트 초기화는 필수 컴포넌트 보강에서 시작합니다.
        EnsureRequiredComponents();
        CacheReferences();
        RegisterServices();

        Status.Initialize(Controller, CheckpointTracker);
        HealItemInventory.Initialize(Status);
        CheckpointTracker.Initialize(Status, HealItemInventory);
        LifeTracker.Initialize();
        ExperienceTracker.Initialize();
        Movement.Initialize(Status);
        Attack.Initialize(Status);
        Parry.Initialize(Controller);
        Controller.Initialize(Movement, Attack, Parry, HealItemInventory);

        Controller.EnterInitialState();
        Status.PublishInitialHealth();
        HealItemInventory.PublishInitialCount();
        LifeTracker.PublishInitialLife();
        ExperienceTracker.PublishInitialExperience();
    }

    private void EnsureRequiredComponents()
    {
        EnsureComponent<CharacterController>();
        EnsureComponent<PlayerController>();
        EnsureComponent<PlayerStatus>();
        EnsureComponent<PlayerMovement>();
        EnsureComponent<PlayerAttack>();
        EnsureComponent<PlayerParry>();
        EnsureComponent<PlayerCheckpointTracker>();
        EnsureComponent<PlayerHealItemInventory>();
        EnsureComponent<PlayerLifeTracker>();
        EnsureComponent<PlayerExperienceTracker>();
        EnsureComponent<HitFlashFeedback>();
    }

    private void CacheReferences()
    {
        Controller = GetComponent<PlayerController>();
        Status = GetComponent<PlayerStatus>();
        Movement = GetComponent<PlayerMovement>();
        Attack = GetComponent<PlayerAttack>();
        Parry = GetComponent<PlayerParry>();
        CheckpointTracker = GetComponent<PlayerCheckpointTracker>();
        HealItemInventory = GetComponent<PlayerHealItemInventory>();
        LifeTracker = GetComponent<PlayerLifeTracker>();
        ExperienceTracker = GetComponent<PlayerExperienceTracker>();
        HitFeedback = GetComponent<HitFlashFeedback>();
    }

    private void RegisterServices()
    {
        ServiceLocator.Register(typeof(PlayerInitializer), this);
        ServiceLocator.Register(typeof(PlayerController), Controller);
        ServiceLocator.Register(typeof(PlayerStatus), Status);
        ServiceLocator.Register(typeof(PlayerMovement), Movement);
        ServiceLocator.Register(typeof(PlayerAttack), Attack);
        ServiceLocator.Register(typeof(PlayerParry), Parry);
        ServiceLocator.Register(typeof(PlayerCheckpointTracker), CheckpointTracker);
        ServiceLocator.Register(typeof(PlayerHealItemInventory), HealItemInventory);
        ServiceLocator.Register(typeof(PlayerLifeTracker), LifeTracker);
        ServiceLocator.Register(typeof(PlayerExperienceTracker), ExperienceTracker);
        ServiceLocator.Register(typeof(HitFlashFeedback), HitFeedback);
    }

    private T EnsureComponent<T>() where T : Component
    {
        T component = GetComponent<T>();

        if (component != null) return component;

        return gameObject.AddComponent<T>();
    }
}
