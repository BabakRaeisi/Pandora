using UnityEngine;

[CreateAssetMenu(menuName = "SWM/Bridge Config", fileName = "BridgeConfig")]
public class BridgeConfig : ScriptableObject
{
    [System.Serializable]
    public struct LevelConfig
    {
        public int levelNumber;       // 1..16
        public int trials;            // per session level

        [Header("Sequence Difficulty")]
        public int minPieces;         // e.g. 3
        public int maxPieces;         // e.g. 6

        [Header("Timing")]
        public int displayMs;         // e.g. 1200 / 1000 / 800
        public int gapMs;             // e.g. 300

        [Header("Pattern / Environment")]
        public BridgePattern pattern;
        public bool allowEnvironmentFX;  // week 4 style modifiers

        [Header("User Messages (FA)")]
        [TextArea] public string successMessageFa;
        [TextArea] public string assistedPassInfoMessageFa;
        [TextArea] public string levelUpInfoMessageFa;
    }

    public LevelConfig[] levels = new LevelConfig[]
    {
        new LevelConfig { levelNumber=1, trials=5, minPieces=3, maxPieces=4, displayMs=1200, gapMs=300, pattern=BridgePattern.Straight,     allowEnvironmentFX=false },
        new LevelConfig { levelNumber=2, trials=5, minPieces=3, maxPieces=4, displayMs=1200, gapMs=300, pattern=BridgePattern.Straight,     allowEnvironmentFX=false },
        new LevelConfig { levelNumber=3, trials=6, minPieces=4, maxPieces=5, displayMs=1000, gapMs=300, pattern=BridgePattern.GentleCurve,  allowEnvironmentFX=false },
        new LevelConfig { levelNumber=4, trials=6, minPieces=4, maxPieces=5, displayMs=1000, gapMs=300, pattern=BridgePattern.ZigZag,       allowEnvironmentFX=false },
        new LevelConfig { levelNumber=5, trials=7, minPieces=5, maxPieces=6, displayMs=900,  gapMs=300, pattern=BridgePattern.LShape,       allowEnvironmentFX=false },
        new LevelConfig { levelNumber=6, trials=7, minPieces=6, maxPieces=7, displayMs=800,  gapMs=300, pattern=BridgePattern.Elevated,     allowEnvironmentFX=true  },
        new LevelConfig { levelNumber=7, trials=8, minPieces=7, maxPieces=8, displayMs=800,  gapMs=300, pattern=BridgePattern.Elevated,     allowEnvironmentFX=true  },
        new LevelConfig { levelNumber=8, trials=5, minPieces=3, maxPieces=4, displayMs=1200, gapMs=300, pattern=BridgePattern.Straight,     allowEnvironmentFX=false },
        new LevelConfig { levelNumber=9, trials=5, minPieces=3, maxPieces=4, displayMs=1200, gapMs=300, pattern=BridgePattern.Straight,     allowEnvironmentFX=false },
        new LevelConfig { levelNumber=10, trials=6, minPieces=4, maxPieces=5, displayMs=1000, gapMs=300, pattern=BridgePattern.GentleCurve,  allowEnvironmentFX=false },
        new LevelConfig { levelNumber=11, trials=6, minPieces=4, maxPieces=5, displayMs=1000, gapMs=300, pattern=BridgePattern.ZigZag,       allowEnvironmentFX=false },
        new LevelConfig { levelNumber=12, trials=7, minPieces=5, maxPieces=6, displayMs=900,  gapMs=300, pattern=BridgePattern.LShape,       allowEnvironmentFX=false },
        new LevelConfig { levelNumber=13, trials=7, minPieces=6, maxPieces=7, displayMs=800,  gapMs=300, pattern=BridgePattern.Elevated,     allowEnvironmentFX=true  },
        new LevelConfig { levelNumber=14, trials=8, minPieces=7, maxPieces=8, displayMs=800,  gapMs=300, pattern=BridgePattern.Elevated,     allowEnvironmentFX=true  },
        new LevelConfig { levelNumber=15, trials=5, minPieces=3, maxPieces=4, displayMs=1200, gapMs=300, pattern=BridgePattern.Straight,     allowEnvironmentFX=false },
        new LevelConfig { levelNumber=16, trials=5, minPieces=3, maxPieces=4, displayMs=1200, gapMs=300, pattern=BridgePattern.Straight,     allowEnvironmentFX=false },
    };

    public LevelConfig GetLevel(int levelNumber)
    {
        if (levels == null) return default;
        levelNumber = Mathf.Clamp(levelNumber, 1, 16);

        for (int i = 0; i < levels.Length; i++)
            if (levels[i].levelNumber == levelNumber) return levels[i];

        return levels[levelNumber - 1];
    }

    public string GetSuccessMessage(LevelConfig level, int spanOrCount) =>
        SafeFormat(level.successMessageFa, "مرحله را با موفقیت کامل کردی", spanOrCount);

    public string GetAssistedPassInfo(LevelConfig level) =>
        string.IsNullOrWhiteSpace(level.assistedPassInfoMessageFa)
            ? "عبور کمکی فعال شد"
            : level.assistedPassInfoMessageFa;

    public string GetLevelUpInfo(LevelConfig level, int nextLevel) =>
        SafeFormat(level.levelUpInfoMessageFa, "ارتقا به مرحله {0}", nextLevel);

    private static string SafeFormat(string pattern, string fallback, params object[] args)
    {
        var p = string.IsNullOrWhiteSpace(pattern) ? fallback : pattern;
        try { return string.Format(p, args); }
        catch { return string.Format(fallback, args); }
    }

    public enum BridgePattern
    {
        Straight,
        GentleCurve,
        ZigZag,
        LShape,
        Elevated
    }
}