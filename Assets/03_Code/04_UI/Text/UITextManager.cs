using UnityEngine;

// 2026.08.10_UI 문구 중앙화
public static class UITextManager
{
    private const string CatalogResourcePath = "Datas/UI/UITextCatalog";
    private static UITextCatalog cachedCatalog;

    // 2026.08.10_UI 문구 중앙화
    // 카탈로그 키로 UI 문구를 조회하고, 누락된 키는 화면과 Console에서 확인할 수 있게 반환한다.
    public static string Get(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            Debug.LogWarning("UITextManager에 빈 문구 키가 전달되었습니다.");
            return string.Empty;
        }

        UITextCatalog catalog = GetCatalog();

        if (catalog != null && catalog.TryGet(key, out string text))
            return text;

        Debug.LogWarning($"UITextCatalog에 등록되지 않은 문구 키입니다: {key}");
        return $"[{key}]";
    }

    // 2026.08.10_UI 문구 중앙화
    // Resources에 있는 단일 UI 문구 카탈로그를 지연 로드한다.
    private static UITextCatalog GetCatalog()
    {
        if (cachedCatalog == null)
            cachedCatalog = Resources.Load<UITextCatalog>(CatalogResourcePath);

        return cachedCatalog;
    }
}
