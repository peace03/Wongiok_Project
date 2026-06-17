using UnityEngine;
using UnityEngine.SceneManagement;

// 테스트 씬 전환
public class PrototypeSceneBridge : MonoBehaviour
{
    [SerializeField] private string inGameSceneName = "InGamePrototype";

    private void OnEnable()
    {
        EventBus<UIChapterEnterRequestedEvent>.action += HandleChapterEnterRequested;
    }

    private void OnDisable()
    {
        EventBus<UIChapterEnterRequestedEvent>.action -= HandleChapterEnterRequested;
    }

    private void HandleChapterEnterRequested(UIChapterEnterRequestedEvent eventData)
    {
        EventBus<UIChangeScreenEvent>.Publish(
            new UIChangeScreenEvent(UIScreenState.Loading));

        EventBus<UISetLoadingProgressEvent>.Publish(
            new UISetLoadingProgressEvent(1f, "인게임 씬 이동 중"));

        SceneManager.LoadScene(inGameSceneName);
    }
}
