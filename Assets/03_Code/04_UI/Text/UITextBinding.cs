using UnityEngine;
using UnityEngine.UI;

// 2026.08.10_UI 문구 중앙화
[DisallowMultipleComponent]
[AddComponentMenu("GRIMOIRE/UI/Text Binding")]
public sealed class UITextBinding : MonoBehaviour
{
    [SerializeField] private Text targetText;
    [SerializeField] private string textKey;

    // 2026.08.10_UI 문구 중앙화
    // 같은 오브젝트의 Text를 기본 참조로 설정한다.
    private void Reset()
    {
        targetText = GetComponent<Text>();
    }

    // 2026.08.10_UI 문구 중앙화
    // 오브젝트가 활성화될 때 Inspector 키의 최신 문구를 표시한다.
    private void OnEnable()
    {
        Refresh();
    }

    // 2026.08.10_UI 문구 중앙화
    // Inspector에 지정한 키의 문구를 대상 Text에 반영한다.
    public void Refresh()
    {
        if (targetText == null || string.IsNullOrWhiteSpace(textKey))
            return;

        targetText.text = UITextManager.Get(textKey);
    }
}
