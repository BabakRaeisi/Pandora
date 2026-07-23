using UnityEngine;

[CreateAssetMenu(menuName = "SWM/Bridge Config", fileName = "BridgeConfig")]
public class BridgeConfig : ScriptableObject
{
    [System.Serializable]
    public struct TitledMessageFa
    {
        public string title;
        [TextArea] public string message;
    }

    [System.Serializable]
    public struct LevelConfig
    {
        public int levelNumber;       // 1..16
        public int trials;            // per session level

        [Header("Progression")]
        public bool isGatewayLevel;   // optional gateway marker
        [Min(0f)] public float lockDurationHours;

        [Header("Sequence Difficulty")]
        public int minPieces;         // e.g. 3
        public int maxPieces;         // e.g. 6

        [Header("Timing")]
        public int displayMs;         // e.g. 1200 / 1000 / 800
        public int gapMs;             // e.g. 300

        [Header("Pattern / Environment")]
        public BridgePattern pattern;
        public bool allowEnvironmentFX;

        [Header("Wrong Pattern (FA)")]
        public TitledMessageFa[] wrongPatternMessagesFa;

        [Header("Trial Success (FA)")]
        public string successTitleFa;
        public TitledMessageFa[] trialSuccessMessagesFa;

        // Legacy fields (kept for compatibility)
        [Header("Legacy User Messages (FA)")]
        [TextArea] public string successMessageFa;
        [TextArea] public string assistedPassInfoMessageFa;
        [TextArea] public string levelUpInfoMessageFa;
    }

    public LevelConfig[] levels = CreateDefaultLevels();

    private static readonly TitledMessageFa[] DefaultWrongPatternMessagesFa = new TitledMessageFa[]
    {
        new TitledMessageFa { title = "اشتباه شد", message = "این یکی درست نبود،دوباره با آرامش امتحان کن." },
        new TitledMessageFa { title = "اشکالی ندارد", message = "اشکالی ندارد،یک بار دیگر تلاش کنیم." },
        new TitledMessageFa { title = "ادامه بده", message = "خیلی خوب پیش می‌روی،این الگو را دوباره بزن." }
    };

    private static readonly TitledMessageFa[] DefaultTrialSuccessMessagesFa = new TitledMessageFa[]
    {
        new TitledMessageFa { title = "آفرین", message = "عالی بود،همین‌طور ادامه بده." },
        new TitledMessageFa { title = "خیلی خوب", message = "خیلی خوب انجامش دادی." },
        new TitledMessageFa { title = "درست بود", message = "درست بود،بریم بعدی." }
    };

    [Header("Level Complete Success Messages (shared)")]
    public string[] levelSuccessMessagesFa = new string[]
    {
        "عالی بود،این مرحله را خیلی خوب رد کردی.",
        "مرحله تمام شد و تو خیلی خوب پیش رفتی.",
        "با موفقیت از این مرحله عبور کردی.",
        "این مرحله را با تمرکز و دقت پشت سر گذاشتی."
    };

    [Header("Assisted Pass Messages (shared)")]
    public string[] assistedPassMessagesFa = new string[]
    {
        "این مرحله با کمک کامل شد،ادامه بده.",
        "با کمک از این مرحله هم عبور کردی.",
        "مرحله برایت کامل شد،تو داری خوب جلو می‌روی.",
        "با هم از این مرحله هم رد شدیم."
    };

    [Header("Gateway Success Message (shared)")]
    [TextArea] public string gatewaySuccessMessageFa = "عالی بود،مرحله کلیدی را رد کردی و کلید را گرفتی.";

    private void OnValidate()
    {
        // Existing ScriptableObject assets keep their serialized values.
        // Restore the default 10 levels if this asset has an empty list.
        if (levels == null || levels.Length == 0)
        {
            levels = CreateDefaultLevels();
        }

        for (int i = 0; i < levels.Length; i++)
        {
            var lv = levels[i];

            if (string.IsNullOrWhiteSpace(lv.successTitleFa))
                lv.successTitleFa = "آفرین";

            if (lv.wrongPatternMessagesFa == null || lv.wrongPatternMessagesFa.Length == 0)
                lv.wrongPatternMessagesFa = (TitledMessageFa[])DefaultWrongPatternMessagesFa.Clone();

            if (lv.trialSuccessMessagesFa == null || lv.trialSuccessMessagesFa.Length == 0)
                lv.trialSuccessMessagesFa = (TitledMessageFa[])DefaultTrialSuccessMessagesFa.Clone();

            levels[i] = lv;
        }

        if (levelSuccessMessagesFa == null || levelSuccessMessagesFa.Length == 0)
            levelSuccessMessagesFa = new[] { "مرحله را کامل رد کردی." };

        if (assistedPassMessagesFa == null || assistedPassMessagesFa.Length == 0)
            assistedPassMessagesFa = new[] { "این مرحله با کمک کامل شد." };
    }

    public LevelConfig GetLevel(int levelNumber)
    {
        if (levels == null || levels.Length == 0)
            return default;

        levelNumber = Mathf.Clamp(levelNumber, 1, levels.Length);

        for (int i = 0; i < levels.Length; i++)
            if (levels[i].levelNumber == levelNumber) return levels[i];

        return levels[Mathf.Clamp(levelNumber - 1, 0, levels.Length - 1)];
    }

    public bool IsGatewayLevel(LevelConfig level) => level.isGatewayLevel;

    public string GetSuccessTitle(LevelConfig level) =>
        string.IsNullOrWhiteSpace(level.successTitleFa) ? "آفرین" : level.successTitleFa;

    public TitledMessageFa GetRandomWrongPattern(LevelConfig level)
    {
        var arr = level.wrongPatternMessagesFa;
        if (arr == null || arr.Length == 0)
            return DefaultWrongPatternMessagesFa[Random.Range(0, DefaultWrongPatternMessagesFa.Length)];

        var item = arr[Random.Range(0, arr.Length)];
        if (string.IsNullOrWhiteSpace(item.message))
            return DefaultWrongPatternMessagesFa[Random.Range(0, DefaultWrongPatternMessagesFa.Length)];

        return item;
    }

    public TitledMessageFa GetRandomTrialSuccess(LevelConfig level)
    {
        var arr = level.trialSuccessMessagesFa;
        if (arr == null || arr.Length == 0)
            return DefaultTrialSuccessMessagesFa[Random.Range(0, DefaultTrialSuccessMessagesFa.Length)];

        var item = arr[Random.Range(0, arr.Length)];
        if (string.IsNullOrWhiteSpace(item.message))
            return DefaultTrialSuccessMessagesFa[Random.Range(0, DefaultTrialSuccessMessagesFa.Length)];

        return item;
    }

    public string GetRandomLevelSuccessMessage()
    {
        if (levelSuccessMessagesFa == null || levelSuccessMessagesFa.Length == 0)
            return "مرحله را کامل رد کردی.";

        return levelSuccessMessagesFa[Random.Range(0, levelSuccessMessagesFa.Length)];
    }

    public string GetRandomAssistedPassMessage()
    {
        if (assistedPassMessagesFa == null || assistedPassMessagesFa.Length == 0)
            return "این مرحله با کمک کامل شد.";

        return assistedPassMessagesFa[Random.Range(0, assistedPassMessagesFa.Length)];
    }

    public string GetFinalSuccessMessage(LevelConfig level)
    {
        if (IsGatewayLevel(level))
            return string.IsNullOrWhiteSpace(gatewaySuccessMessageFa)
                ? "عالی بود،مرحله کلیدی را رد کردی و کلید را گرفتی."
                : gatewaySuccessMessageFa;

        return GetRandomLevelSuccessMessage();
    }

    // Compatibility methods (existing callers can keep using these)
    public string GetSuccessMessage(LevelConfig level, int spanOrCount)
    {
        if (!string.IsNullOrWhiteSpace(level.successMessageFa))
            return SafeFormat(level.successMessageFa, "مرحله را با موفقیت کامل کردی", spanOrCount);

        return GetRandomTrialSuccess(level).message;
    }

    public string GetAssistedPassInfo(LevelConfig level)
    {
        if (!string.IsNullOrWhiteSpace(level.assistedPassInfoMessageFa))
            return level.assistedPassInfoMessageFa;

        return GetRandomAssistedPassMessage();
    }

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

    private static LevelConfig[] CreateDefaultLevels()
    {
        return new[]
        {
            // Steps 2-3.
            new LevelConfig { levelNumber = 1,  trials = 3, minPieces = 2,  maxPieces = 2,  displayMs = 1500, gapMs = 400, pattern = BridgePattern.Straight,    allowEnvironmentFX = false, successTitleFa = "آفرین" },
            new LevelConfig { levelNumber = 2,  trials = 3, minPieces = 2,  maxPieces = 3,  displayMs = 1450, gapMs = 380, pattern = BridgePattern.Straight,    allowEnvironmentFX = false, successTitleFa = "آفرین" },

            // Steps 3-5.
            new LevelConfig { levelNumber = 3,  trials = 4, minPieces = 3,  maxPieces = 4,  displayMs = 1350, gapMs = 350, pattern = BridgePattern.GentleCurve, allowEnvironmentFX = false, successTitleFa = "آفرین" },
            new LevelConfig { levelNumber = 4,  trials = 4, minPieces = 4,  maxPieces = 5,  displayMs = 1250, gapMs = 325, pattern = BridgePattern.ZigZag,      allowEnvironmentFX = false, successTitleFa = "آفرین" },

            // Steps 5-6.
            new LevelConfig { levelNumber = 5,  trials = 5, minPieces = 5,  maxPieces = 6,  displayMs = 1150, gapMs = 300, pattern = BridgePattern.LShape,      allowEnvironmentFX = false, successTitleFa = "آفرین", isGatewayLevel = true },

            // Steps 6-8.
            new LevelConfig { levelNumber = 6,  trials = 5, minPieces = 6,  maxPieces = 7,  displayMs = 1050, gapMs = 275, pattern = BridgePattern.GentleCurve, allowEnvironmentFX = true,  successTitleFa = "آفرین" },
            new LevelConfig { levelNumber = 7,  trials = 6, minPieces = 7,  maxPieces = 8,  displayMs = 950,  gapMs = 250, pattern = BridgePattern.ZigZag,      allowEnvironmentFX = true,  successTitleFa = "آفرین" },

            // Steps 8-10.
            new LevelConfig { levelNumber = 8,  trials = 6, minPieces = 8,  maxPieces = 9,  displayMs = 850,  gapMs = 225, pattern = BridgePattern.LShape,      allowEnvironmentFX = true,  successTitleFa = "آفرین" },
            new LevelConfig { levelNumber = 9,  trials = 7, minPieces = 9,  maxPieces = 10, displayMs = 750,  gapMs = 200, pattern = BridgePattern.Elevated,    allowEnvironmentFX = true,  successTitleFa = "آفرین" },

            // Steps 10-12.
            new LevelConfig { levelNumber = 10, trials = 8, minPieces = 10, maxPieces = 12, displayMs = 650,  gapMs = 175, pattern = BridgePattern.Elevated,    allowEnvironmentFX = true,  successTitleFa = "آفرین", isGatewayLevel = true },
        };
    }
}