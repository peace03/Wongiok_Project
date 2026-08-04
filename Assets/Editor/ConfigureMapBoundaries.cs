using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ConfigureMapBoundaries
{
    private const string ChapterScenePath =
        "Assets/04_Level/Scenes/Real/Chapter1Scene.unity";
    private const string BossScenePath =
        "Assets/04_Level/Scenes/Real/RougeHoodBossScene.unity";

    [MenuItem("Tools/Player/Configure Map Boundaries")]
    public static void Configure()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        ConfigureScene(ChapterScenePath, 0f, 210f);
        ConfigureScene(BossScenePath, -20f, 20f);
        AssetDatabase.SaveAssets();

        Debug.Log(
            "[MapBoundary] Chapter1Scene과 RougeHoodBossScene 경계를 생성했습니다.");
    }

    private static void ConfigureScene(
        string scenePath,
        float playableMinX,
        float playableMaxX)
    {
        Scene scene = EditorSceneManager.OpenScene(
            scenePath,
            OpenSceneMode.Single);

        GameObject root = scene.GetRootGameObjects()
            .FirstOrDefault(candidate => candidate.name == "MapBoundary");

        if (root == null)
        {
            root = new GameObject("MapBoundary");
            SceneManager.MoveGameObjectToScene(root, scene);
        }

        while (root.transform.childCount > 0)
        {
            Object.DestroyImmediate(root.transform.GetChild(0).gameObject);
        }

        CreateWall(root.transform, "LeftBoundary", playableMinX - 0.5f);
        CreateWall(root.transform, "RightBoundary", playableMaxX + 0.5f);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void CreateWall(
        Transform parent,
        string objectName,
        float centerX)
    {
        GameObject wall = new(objectName);
        wall.transform.SetParent(parent, false);
        wall.transform.localPosition = new Vector3(centerX, 10f, 0f);

        BoxCollider collider = wall.AddComponent<BoxCollider>();
        collider.isTrigger = false;
        collider.size = new Vector3(1f, 40f, 10f);
    }
}
