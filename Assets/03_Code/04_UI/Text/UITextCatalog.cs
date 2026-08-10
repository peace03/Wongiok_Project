using System;
using UnityEngine;

// 2026.08.10_UI 문구 중앙화
[Serializable]
public sealed class UITextEntry
{
    [SerializeField] private string key;
    [TextArea(2, 5)] [SerializeField] private string text;

    public string Key => key;
    public string Text => text;
}

// 2026.08.10_UI 문구 중앙화
[CreateAssetMenu(fileName = "UITextCatalog", menuName = "GRIMOIRE/UI/Text Catalog")]
public sealed class UITextCatalog : ScriptableObject
{
    [SerializeField] private UITextEntry[] entries = Array.Empty<UITextEntry>();

    // 2026.08.10_UI 문구 중앙화
    // 지정한 키에 등록된 문구를 반환한다.
    public bool TryGet(string key, out string text)
    {
        if (string.IsNullOrWhiteSpace(key) || entries == null)
        {
            text = null;
            return false;
        }

        for (int i = 0; i < entries.Length; i++)
        {
            UITextEntry entry = entries[i];

            if (entry == null || entry.Key != key)
                continue;

            text = entry.Text ?? string.Empty;
            return true;
        }

        text = null;
        return false;
    }
}
