using UnityEngine;

[CreateAssetMenu(menuName = "PandoraBox/Constellation/Config", fileName = "ConstellationConfig")]
public class ConstellationConfigSO : ScriptableObject
{
    [System.Serializable]
    public class DayConfig
    {
        [Range(1, 7)] public int dayNumber = 1;

        [Min(1)] public int trials = 7;

        [Header("Difficulty (span = sequence length)")]
        [Range(2, 9)] public int span = 3;

        [Header("Timing (seconds)")]
        public float starOnSeconds = 1.0f;
        public float gapSeconds = 0.25f;
    }

    public DayConfig[] days;

    public DayConfig GetDay(int dayNumber)
    {
        if (days == null) return null;
        for (int i = 0; i < days.Length; i++)
            if (days[i] != null && days[i].dayNumber == dayNumber)
                return days[i];
        return null;
    }
}
