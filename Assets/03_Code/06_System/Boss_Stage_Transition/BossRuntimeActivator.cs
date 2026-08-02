using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class BossRuntimeActivator : MonoBehaviour
{
    [Header("Boss")]
    [SerializeField] private GameObject bossRoot;

    [Header("Battle Objects")]
    [SerializeField] private GameObject[] objectsToEnableForBattle;
    [SerializeField] private GameObject[] objectsToDisableForBattle;

    [Header("Events")]
    [SerializeField] private UnityEvent onBossActivated;

    private bool isInitialized;

    public bool IsInitialized => isInitialized;

    // 보스와 보스전 전용 오브젝트를 비활성 상태로 준비합니다.
    private void Awake()
    {
        if (bossRoot != null)
        {
            bossRoot.SetActive(false);
        }

        SetObjectsActive(objectsToEnableForBattle, false);
    }

    // 비활성 보스를 켜고 기존 Bootstrapper와 같은 순서로 초기화합니다.
    public void ActivateAndInitialize()
    {
        if (isInitialized)
        {
            return;
        }

        if (bossRoot == null)
        {
            Debug.LogError(
                "Boss Root 참조가 비어 있습니다.",
                this);
            return;
        }

        SetObjectsActive(objectsToDisableForBattle, false);
        SetObjectsActive(objectsToEnableForBattle, true);

        bossRoot.SetActive(true);
        InitializeBossComponents();

        isInitialized = true;
        onBossActivated?.Invoke();
    }

    // 보스 루트 아래의 초기화 대상들을 우선순위 순서대로 실행합니다.
    private void InitializeBossComponents()
    {
        IInitializable[] managers =
            bossRoot
                .GetComponentsInChildren<MonoBehaviour>(true)
                .Where(component =>
                    component != null &&
                    component.gameObject.activeInHierarchy)
                .OfType<IInitializable>()
                .OrderBy(manager => manager.Priority)
                .ToArray();

        for (int i = 0; i < managers.Length; i++)
        {
            RegisterAndInitialize(managers[i]);
        }
    }

    // 초기화 대상을 ServiceLocator에 등록하고 Init을 호출합니다.
    private void RegisterAndInitialize(IInitializable manager)
    {
        Type concreteType = manager.GetType();

        ServiceLocator.Register(concreteType, manager);
        manager.Init();

        Type[] interfaces = concreteType.GetInterfaces();

        for (int i = 0; i < interfaces.Length; i++)
        {
            Type interfaceType = interfaces[i];

            if (interfaceType == typeof(IInitializable))
            {
                continue;
            }

            ServiceLocator.Register(interfaceType, manager);
        }
    }

    // 전달된 오브젝트 배열에 같은 활성 상태를 적용합니다.
    private void SetObjectsActive(
        GameObject[] targetObjects,
        bool active)
    {
        if (targetObjects == null)
        {
            return;
        }

        for (int i = 0; i < targetObjects.Length; i++)
        {
            GameObject targetObject = targetObjects[i];

            if (targetObject != null)
            {
                targetObject.SetActive(active);
            }
        }
    }
}
