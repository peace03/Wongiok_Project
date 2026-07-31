using System.Collections.Generic;
using UnityEngine;

public class MonsterDeathHandler : MonoBehaviour
{
    private const string DefaultCorpseLayerName = "Corpse";

    [Header("Death")]
    [SerializeField] private Animator animator;
    [SerializeField] private string deadTriggerName = "Dead";
    [SerializeField] private bool destroyAfterDeath = true;
    [SerializeField] private float destroyDelay = 1.5f;
    [SerializeField] private bool disableCharacterControllerOnDeath = true;

    [Header("Death Collision")]
    [SerializeField] private bool changeBlockingColliderLayerOnDeath = true;
    [SerializeField] private string corpseLayerName = DefaultCorpseLayerName;
    [SerializeField] private Collider[] blockingColliders;

    private CharacterController characterController;
    private int[] originalBlockingColliderLayers;
    private int corpseLayer = -1;
    private bool handled;

    // 컴포넌트가 추가될 때 기본 참조를 준비합니다
    private void Reset()
    {
        animator = GetComponentInChildren<Animator>();
        characterController = GetComponent<CharacterController>();
        corpseLayerName = DefaultCorpseLayerName;

        CacheBlockingColliders();
    }

    // 사망 처리에 필요한 컴포넌트와 Layer 정보를 준비합니다
    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        characterController = GetComponent<CharacterController>();

        CacheBlockingColliders();
        CacheOriginalBlockingColliderLayers();
        CacheCorpseLayer();
    }

    // 몬스터가 활성화될 때 사망 상태와 충돌 Layer를 초기화합니다
    private void OnEnable()
    {
        handled = false;
        RestoreBlockingColliderLayers();

        EventBus<MonsterDeadEvent>.action += HandleMonsterDead;
    }

    // 몬스터가 비활성화될 때 사망 이벤트 구독을 해제합니다
    private void OnDisable()
    {
        EventBus<MonsterDeadEvent>.action -= HandleMonsterDead;
    }

    // 자신에게 해당하는 사망 이벤트만 처리합니다
    private void HandleMonsterDead(MonsterDeadEvent deadEvent)
    {
        if (handled)
        {
            return;
        }

        if (deadEvent.MonsterObject == null)
        {
            return;
        }

        if (!IsSameObjectOrChild(deadEvent.MonsterObject))
        {
            return;
        }

        handled = true;

        DisableControllerIfNeeded();
        ChangeBlockingCollidersToCorpseLayer();
        PlayDeathAnimation();

        if (destroyAfterDeath)
        {
            Destroy(gameObject, Mathf.Max(0f, destroyDelay));
        }
    }

    // 전달된 오브젝트가 자신 또는 자식인지 확인합니다
    private bool IsSameObjectOrChild(GameObject monsterObject)
    {
        if (monsterObject == gameObject)
        {
            return true;
        }

        return monsterObject.transform.IsChildOf(transform) ||
               transform.IsChildOf(monsterObject.transform);
    }

    // 필요하면 CharacterController를 비활성화합니다
    private void DisableControllerIfNeeded()
    {
        if (!disableCharacterControllerOnDeath)
        {
            return;
        }

        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }

        if (characterController != null)
        {
            characterController.enabled = false;
        }
    }

    // 사망한 몬스터의 몸통 Collider를 Corpse Layer로 변경합니다
    private void ChangeBlockingCollidersToCorpseLayer()
    {
        if (!changeBlockingColliderLayerOnDeath)
        {
            return;
        }

        if (corpseLayer < 0)
        {
            CacheCorpseLayer();
        }

        if (corpseLayer < 0)
        {
            Debug.LogWarning(
                "Corpse Layer가 없습니다. Tags and Layers 설정을 확인해주세요.",
                this
            );

            return;
        }

        if (blockingColliders == null || blockingColliders.Length == 0)
        {
            CacheBlockingColliders();
        }

        for (int i = 0; i < blockingColliders.Length; i++)
        {
            Collider blockingCollider = blockingColliders[i];

            if (blockingCollider == null)
            {
                continue;
            }

            if (blockingCollider == characterController)
            {
                continue;
            }

            if (blockingCollider.isTrigger)
            {
                continue;
            }

            blockingCollider.gameObject.layer = corpseLayer;
        }
    }

    // 몸통 충돌에 사용하는 Trigger가 아닌 Collider를 자동으로 찾습니다
    private void CacheBlockingColliders()
    {
        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }

        if (blockingColliders != null && blockingColliders.Length > 0)
        {
            return;
        }

        Collider[] foundColliders =
            GetComponentsInChildren<Collider>(true);

        List<Collider> validColliders =
            new List<Collider>(foundColliders.Length);

        for (int i = 0; i < foundColliders.Length; i++)
        {
            Collider foundCollider = foundColliders[i];

            if (foundCollider == null)
            {
                continue;
            }

            if (foundCollider == characterController)
            {
                continue;
            }

            if (foundCollider.isTrigger)
            {
                continue;
            }

            validColliders.Add(foundCollider);
        }

        blockingColliders = validColliders.ToArray();
    }

    // 재사용을 위해 몸통 Collider의 원래 Layer를 저장합니다
    private void CacheOriginalBlockingColliderLayers()
    {
        if (blockingColliders == null)
        {
            originalBlockingColliderLayers = null;
            return;
        }

        originalBlockingColliderLayers =
            new int[blockingColliders.Length];

        for (int i = 0; i < blockingColliders.Length; i++)
        {
            Collider blockingCollider = blockingColliders[i];

            originalBlockingColliderLayers[i] =
                blockingCollider != null
                    ? blockingCollider.gameObject.layer
                    : -1;
        }
    }

    // 오브젝트가 다시 활성화될 때 기존 충돌 Layer를 복원합니다
    private void RestoreBlockingColliderLayers()
    {
        if (blockingColliders == null ||
            originalBlockingColliderLayers == null)
        {
            return;
        }

        int restoreCount = Mathf.Min(
            blockingColliders.Length,
            originalBlockingColliderLayers.Length
        );

        for (int i = 0; i < restoreCount; i++)
        {
            Collider blockingCollider = blockingColliders[i];
            int originalLayer = originalBlockingColliderLayers[i];

            if (blockingCollider == null || originalLayer < 0)
            {
                continue;
            }

            blockingCollider.gameObject.layer = originalLayer;
        }
    }

    // 사망 충돌에 사용할 Corpse Layer 번호를 저장합니다
    private void CacheCorpseLayer()
    {
        if (string.IsNullOrWhiteSpace(corpseLayerName))
        {
            corpseLayer = -1;
            return;
        }

        corpseLayer = LayerMask.NameToLayer(corpseLayerName);
    }

    // 사망 애니메이션 트리거를 실행합니다
    private void PlayDeathAnimation()
    {
        if (animator == null || string.IsNullOrEmpty(deadTriggerName))
        {
            return;
        }

        animator.SetTrigger(deadTriggerName);
    }
}