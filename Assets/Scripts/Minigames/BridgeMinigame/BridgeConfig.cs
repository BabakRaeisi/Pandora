using UnityEngine;

[CreateAssetMenu(menuName = "SWM/Bridge Config", fileName = "BridgeConfig")]
public class BridgeConfig : ScriptableObject
{
    [System.Serializable]
    public struct DayConfig
    {
        public int day;               // 1..7
        public int trials;            // per session day

        [Header("Sequence Difficulty")]
        public int minPieces;         // e.g. 3
        public int maxPieces;         // e.g. 6

        [Header("Timing")]
        public int displayMs;         // e.g. 1200 / 1000 / 800
        public int gapMs;             // e.g. 300

        [Header("Pattern / Environment")]
        public BridgePattern pattern;
        public bool allowEnvironmentFX;  // week 4 style modifiers
    }

    public DayConfig[] days = new DayConfig[]
    {
        new DayConfig { day=1, trials=5, minPieces=3, maxPieces=4, displayMs=1200, gapMs=300, pattern=BridgePattern.Straight,     allowEnvironmentFX=false },
        new DayConfig { day=2, trials=5, minPieces=3, maxPieces=4, displayMs=1200, gapMs=300, pattern=BridgePattern.Straight,     allowEnvironmentFX=false },
        new DayConfig { day=3, trials=6, minPieces=4, maxPieces=5, displayMs=1000, gapMs=300, pattern=BridgePattern.GentleCurve,  allowEnvironmentFX=false },
        new DayConfig { day=4, trials=6, minPieces=4, maxPieces=5, displayMs=1000, gapMs=300, pattern=BridgePattern.ZigZag,       allowEnvironmentFX=false },
        new DayConfig { day=5, trials=7, minPieces=5, maxPieces=6, displayMs=900,  gapMs=300, pattern=BridgePattern.LShape,       allowEnvironmentFX=false },
        new DayConfig { day=6, trials=7, minPieces=6, maxPieces=7, displayMs=800,  gapMs=300, pattern=BridgePattern.Elevated,     allowEnvironmentFX=true  },
        new DayConfig { day=7, trials=8, minPieces=7, maxPieces=8, displayMs=800,  gapMs=300, pattern=BridgePattern.Elevated,     allowEnvironmentFX=true  },
    };

    public DayConfig GetDay(int day)
    {
        day = Mathf.Clamp(day, 1, 7);
        for (int i = 0; i < days.Length; i++)
            if (days[i].day == day) return days[i];

        return days[day - 1];
    }
}

public enum BridgePattern
{
    Straight,
    GentleCurve,
    ZigZag,
    LShape,
    Elevated
}