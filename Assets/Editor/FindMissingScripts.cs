#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class FindMissingScripts
{
    [MenuItem("Tools/Find Missing Scripts In Open Scenes")]
    private static void FindInOpenScenes()
    {
        int missingCount = 0;

        foreach (GameObject gameObject in Object.FindObjectsByType<GameObject>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
        {
            Component[] components = gameObject.GetComponents<Component>();

            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] != null)
                    continue;

                missingCount++;
                Debug.Log(
                    $"Missing script on: {GetHierarchyPath(gameObject)}",
                    gameObject
                );
            }
        }

        Debug.Log($"Found {missingCount} missing script component(s).");
    }

    private static string GetHierarchyPath(GameObject gameObject)
    {
        string path = gameObject.name;
        Transform parent = gameObject.transform.parent;

        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }

        return path;
    }
}
#endif