using UnityEngine;

[CreateAssetMenu(menuName = "SWM/SWM Config", fileName = "SWMConfig")]
public class SWMConfig : ScriptableObject
{
    [System.Serializable]
    public struct LevelConfig
    {
        public int levelNumber;     // 1..16
        public int boxes;
        public int treasures;
        public int trials;          // per session level

        [Header("User Messages (FA)")]
        [TextArea] public string successMessageFa;
        [TextArea] public string assistedPassInfoMessageFa;
        [TextArea] public string levelUpInfoMessageFa;
    }

    public LevelConfig[] levels = new LevelConfig[]
    {
        new LevelConfig{ levelNumber=1, boxes=3, treasures=2, trials=5 },
        new LevelConfig{ levelNumber=2, boxes=4, treasures=2, trials=5 },
        new LevelConfig{ levelNumber=3, boxes=4, treasures=3, trials=6 },
        new LevelConfig{ levelNumber=4, boxes=6, treasures=3, trials=6 },
        new LevelConfig{ levelNumber=5, boxes=6, treasures=4, trials=7 },
        new LevelConfig{ levelNumber=6, boxes=6, treasures=4, trials=7 },
        new LevelConfig{ levelNumber=7, boxes=8, treasures=4, trials=8 },
        new LevelConfig{ levelNumber=8, boxes=3, treasures=2, trials=5 },
        new LevelConfig{ levelNumber=9, boxes=4, treasures=2, trials=5 },
        new LevelConfig{ levelNumber=10, boxes=4, treasures=3, trials=6 },
        new LevelConfig{ levelNumber=11, boxes=6, treasures=3, trials=6 },
        new LevelConfig{ levelNumber=12, boxes=6, treasures=4, trials=7 },
        new LevelConfig{ levelNumber=13, boxes=6, treasures=4, trials=7 },
        new LevelConfig{ levelNumber=14, boxes=8, treasures=4, trials=8 },
        new LevelConfig{ levelNumber=15, boxes=3, treasures=2, trials=5 },
        new LevelConfig{ levelNumber=16, boxes=4, treasures=2, trials=5 },
    };

    public LevelConfig GetLevel(int levelNumber)
    {
        if (levels == null) return default;
        levelNumber = Mathf.Clamp(levelNumber, 1, 16);

        for (int i = 0; i < levels.Length; i++)
            if (levels[i].levelNumber == levelNumber) return levels[i];

        return levels[levelNumber - 1];
    }

    public string GetSuccessMessage(LevelConfig level, int countOrSpan) =>
        SafeFormat(level.successMessageFa, "مرحله را با موفقیت کامل کردی", countOrSpan);

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
}