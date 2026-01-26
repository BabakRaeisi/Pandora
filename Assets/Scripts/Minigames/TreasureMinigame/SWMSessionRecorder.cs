using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SWMSessionRecorder : MonoBehaviour
{
    [SerializeField] private string participantId = "P001";
    [SerializeField] private string folderName = "swm";

    private readonly List<SWMTrialData> trials = new();

    public void AddTrial(SWMTrialData data) => trials.Add(data);

    public void SaveDay(int day)
    {
        string root = Path.Combine(Application.persistentDataPath, folderName, participantId);
        Directory.CreateDirectory(root);

        string file = Path.Combine(root, $"day_{day:00}.json");
        string json = JsonHelper.ToJson(trials, prettyPrint: true);
        File.WriteAllText(file, json);

        trials.Clear();
    }
}

// Unity can't JsonUtility a top-level list, helper wrapper:
public static class JsonHelper
{
    [System.Serializable] private class Wrapper<T> { public List<T> items; }
    public static string ToJson<T>(List<T> list, bool prettyPrint)
    {
        return JsonUtility.ToJson(new Wrapper<T> { items = list }, prettyPrint);
    }
}
