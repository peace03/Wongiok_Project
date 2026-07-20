using System.Collections.Generic;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CheckpointInteractionZone : MonoBehaviour
{
    [Header("Checkpoint")]
    [SerializeField]
    private CheckpointDefinitionBinder definitionBinder;

    [Header("Prompt")]
    [SerializeField] private CanvasGroup promptCanvasGroup;
    [SerializeField] private TMP_Text promptText;
    [SerializeField]
    private string availableMessage =
        "방향키 위로 체크포인트 활성화";
    [SerializeField]
    private string activatedMessage =
        "방향키 위로 체크포인트 갱신";

    [Header("Behavior")]
    [SerializeField]
    private bool hidePromptAfterActivation;

    private readonly HashSet<Collider> overlappingColliders =
        new HashSet<Collider>();

    private CheckpointPlayerAgent currentAgent;
    private bool hasActivatedBefore;

    public CheckpointDefinition Definition =>
        definitionBinder != null
            ? definitionBinder.Definition
            : null;

    public string PromptText =>
        hasActivatedBefore
            ? activatedMessage
            : availableMessage;

    // Trigger와 Definition 참조 및 안내 UI를 초기화합니다
    private void Awake()
    {
        Collider interactionCollider = GetComponent<Collider>();
        interactionCollider.isTrigger = true;

        if (definitionBinder == null)
        {
            definitionBinder =
                GetComponentInParent<CheckpointDefinitionBinder>();
        }

        SetPromptVisible(false);
        UpdatePromptText();
    }

    // 플레이어가 진입하면 Agent에 이 체크포인트 영역을 등록합니다
    private void OnTriggerEnter(Collider other)
    {
        CheckpointPlayerAgent agent =
            other.GetComponentInParent<CheckpointPlayerAgent>();

        if (agent == null)
        {
            return;
        }

        if (currentAgent != null && currentAgent != agent)
        {
            return;
        }

        overlappingColliders.Add(other);
        currentAgent = agent;
        currentAgent.RegisterZone(this);

        UpdatePromptText();
        SetPromptVisible(CanShowPrompt());
    }

    // 플레이어의 모든 Collider가 빠져나가면 영역 등록을 해제합니다
    private void OnTriggerExit(Collider other)
    {
        CheckpointPlayerAgent agent =
            other.GetComponentInParent<CheckpointPlayerAgent>();

        if (agent == null || agent != currentAgent)
        {
            return;
        }

        overlappingColliders.Remove(other);

        if (overlappingColliders.Count > 0)
        {
            return;
        }

        currentAgent.UnregisterZone(this);
        currentAgent = null;

        SetPromptVisible(false);
    }

    // 컴포넌트가 꺼질 때 Agent 등록과 안내 UI를 정리합니다
    private void OnDisable()
    {
        if (currentAgent != null)
        {
            currentAgent.UnregisterZone(this);
        }

        overlappingColliders.Clear();
        currentAgent = null;
        SetPromptVisible(false);
    }

    // 전달된 Agent가 현재 체크포인트를 활성화할 수 있는지 확인합니다
    public bool CanActivate(CheckpointPlayerAgent agent)
    {
        if (agent == null || agent != currentAgent)
        {
            return false;
        }

        CheckpointDefinition definition = Definition;

        return definition != null && definition.IsValid;
    }

    // 체크포인트 활성화 이벤트를 발행하고 안내 상태를 갱신합니다
    public bool TryActivate(CheckpointPlayerAgent agent)
    {
        if (!CanActivate(agent))
        {
            return false;
        }

        hasActivatedBefore = true;
        UpdatePromptText();
        SetPromptVisible(!hidePromptAfterActivation);

        EventBus<StageCheckpointActivatedEvent>.Publish(
            new StageCheckpointActivatedEvent(
                agent.gameObject,
                definitionBinder.gameObject,
                Definition));

        return true;
    }

    // 현재 활성화 상태에 맞는 안내 문구를 반영합니다
    private void UpdatePromptText()
    {
        if (promptText == null)
        {
            return;
        }

        promptText.text = PromptText;
    }

    // 현재 상태에서 안내 UI를 표시할 수 있는지 확인합니다
    private bool CanShowPrompt()
    {
        if (currentAgent == null)
        {
            return false;
        }

        if (hasActivatedBefore &&
            hidePromptAfterActivation)
        {
            return false;
        }

        return true;
    }

    // World Space 안내 Canvas의 표시 상태를 변경합니다
    private void SetPromptVisible(bool visible)
    {
        if (promptCanvasGroup == null)
        {
            return;
        }

        promptCanvasGroup.alpha = visible ? 1f : 0f;
        promptCanvasGroup.interactable = false;
        promptCanvasGroup.blocksRaycasts = false;
    }
}
