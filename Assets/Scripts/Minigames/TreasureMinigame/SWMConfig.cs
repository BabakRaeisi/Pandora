using UnityEngine;

[CreateAssetMenu(menuName = "SWM/SWM Config", fileName = "SWMConfig")]
public class SWMConfig : ScriptableObject
{
    [System.Serializable]
    public struct TitledMessageFa
    {
        public string title;

        [TextArea]
        public string message;
    }

    [System.Serializable]
    public struct LevelConfig
    {
        [Header("Level")]
        public int levelNumber; // 1..16
        public int boxes;
        public int treasures;
        public int trials;

        [Header("Progression")]
        public bool isGatewayLevel;
        [Min(0f)] public float lockDurationHours;

        [Header("Wrong Pattern (FA)")]
        public TitledMessageFa[] wrongPatternMessagesFa;

        [Header("Trial Success (FA)")]
        public string successTitleFa;
        public TitledMessageFa[] trialSuccessMessagesFa;

        // Retained for compatibility with existing callers/ScriptableObject data.
        [Header("Legacy User Messages (FA)")]
        [TextArea] public string successMessageFa;
        [TextArea] public string assistedPassInfoMessageFa;
        [TextArea] public string levelUpInfoMessageFa;
    }

    public LevelConfig[] levels =
    {
        new LevelConfig { levelNumber = 1, boxes = 3, treasures = 2, trials = 5, successTitleFa = "آفرین" },
        new LevelConfig { levelNumber = 2, boxes = 4, treasures = 2, trials = 5, successTitleFa = "آفرین" },
        new LevelConfig { levelNumber = 3, boxes = 4, treasures = 3, trials = 6, successTitleFa = "آفرین" },
        new LevelConfig { levelNumber = 4, boxes = 6, treasures = 3, trials = 6, successTitleFa = "آفرین" },
        new LevelConfig { levelNumber = 5, boxes = 6, treasures = 4, trials = 7, successTitleFa = "آفرین" },
        new LevelConfig { levelNumber = 6, boxes = 6, treasures = 4, trials = 7, successTitleFa = "آفرین" },
        new LevelConfig { levelNumber = 7, boxes = 8, treasures = 4, trials = 8, successTitleFa = "آفرین" },
        new LevelConfig { levelNumber = 8, boxes = 3, treasures = 2, trials = 5, successTitleFa = "آفرین" },
        new LevelConfig { levelNumber = 9, boxes = 4, treasures = 2, trials = 5, successTitleFa = "آفرین" },
        new LevelConfig { levelNumber = 10, boxes = 4, treasures = 3, trials = 6, successTitleFa = "آفرین" },
        new LevelConfig { levelNumber = 11, boxes = 6, treasures = 3, trials = 6, successTitleFa = "آفرین" },
        new LevelConfig { levelNumber = 12, boxes = 6, treasures = 4, trials = 7, successTitleFa = "آفرین" },
        new LevelConfig { levelNumber = 13, boxes = 6, treasures = 4, trials = 7, successTitleFa = "آفرین" },
        new LevelConfig { levelNumber = 14, boxes = 8, treasures = 4, trials = 8, successTitleFa = "آفرین" },
        new LevelConfig { levelNumber = 15, boxes = 3, treasures = 2, trials = 5, successTitleFa = "آفرین" },
        new LevelConfig { levelNumber = 16, boxes = 4, treasures = 2, trials = 5, successTitleFa = "آفرین" }
    };

    private static readonly TitledMessageFa[] DefaultWrongPatternMessagesFa =
    {
        new TitledMessageFa
        {
            title = "اشتباه شد",
            message = "این یکی درست نبود، دوباره با آرامش امتحان کن."
        },
        new TitledMessageFa
        {
            title = "اشکالی ندارد",
            message = "اشکالی ندارد، یک بار دیگر تلاش کنیم."
        },
        new TitledMessageFa
        {
            title = "ادامه بده",
            message = "خیلی خوب پیش می‌روی، جای گنج‌ها را دوباره به یاد بیاور."
        }
    };

    private static readonly TitledMessageFa[] DefaultTrialSuccessMessagesFa =
    {
        new TitledMessageFa
        {
            title = "آفرین",
            message = "عالی بود، جای گنج‌ها را درست پیدا کردی."
        },
        new TitledMessageFa
        {
            title = "خیلی خوب",
            message = "خیلی خوب انجامش دادی."
        },
        new TitledMessageFa
        {
            title = "درست بود",
            message = "درست بود، بریم سراغ بعدی."
        }
    };

    [Header("Level Complete Success Messages (shared)")]
    public string[] levelSuccessMessagesFa =
    {
        "عالی بود، این مرحله را خیلی خوب رد کردی.",
        "مرحله تمام شد و تو خیلی خوب پیش رفتی.",
        "با موفقیت از این مرحله عبور کردی.",
        "این مرحله را با تمرکز و دقت پشت سر گذاشتی."
    };

    [Header("Assisted Pass Messages (shared)")]
    public string[] assistedPassMessagesFa =
    {
        "این مرحله با کمک کامل شد، ادامه بده.",
        "با کمک از این مرحله هم عبور کردی.",
        "مرحله برایت کامل شد، تو داری خوب جلو می‌روی.",
        "با هم از این مرحله هم رد شدیم."
    };

    [Header("Gateway Success Message (shared)")]
    [TextArea]
    public string gatewaySuccessMessageFa =
        "عالی بود، مرحله کلیدی را رد کردی و کلید را گرفتی.";

    private void OnValidate()
    {
        if (levels != null)
        {
            for (int i = 0; i < levels.Length; i++)
            {
                LevelConfig level = levels[i];

                if (string.IsNullOrWhiteSpace(level.successTitleFa))
                    level.successTitleFa = "آفرین";

                if (level.wrongPatternMessagesFa == null ||
                    level.wrongPatternMessagesFa.Length == 0)
                {
                    level.wrongPatternMessagesFa =
                        (TitledMessageFa[])DefaultWrongPatternMessagesFa.Clone();
                }

                if (level.trialSuccessMessagesFa == null ||
                    level.trialSuccessMessagesFa.Length == 0)
                {
                    level.trialSuccessMessagesFa =
                        (TitledMessageFa[])DefaultTrialSuccessMessagesFa.Clone();
                }

                levels[i] = level;
            }
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
        {
            if (levels[i].levelNumber == levelNumber)
                return levels[i];
        }

        return levels[Mathf.Clamp(levelNumber - 1, 0, levels.Length - 1)];
    }

    public bool IsGatewayLevel(LevelConfig level)
    {
        return level.isGatewayLevel;
    }

    public string GetSuccessTitle(LevelConfig level)
    {
        return string.IsNullOrWhiteSpace(level.successTitleFa)
            ? "آفرین"
            : level.successTitleFa;
    }

    public TitledMessageFa GetRandomWrongPattern(LevelConfig level)
    {
        TitledMessageFa[] messages = level.wrongPatternMessagesFa;

        if (messages == null || messages.Length == 0)
        {
            return DefaultWrongPatternMessagesFa[
                Random.Range(0, DefaultWrongPatternMessagesFa.Length)
            ];
        }

        TitledMessageFa message = messages[Random.Range(0, messages.Length)];

        if (string.IsNullOrWhiteSpace(message.message))
        {
            return DefaultWrongPatternMessagesFa[
                Random.Range(0, DefaultWrongPatternMessagesFa.Length)
            ];
        }

        return message;
    }

    public TitledMessageFa GetRandomTrialSuccess(LevelConfig level)
    {
        TitledMessageFa[] messages = level.trialSuccessMessagesFa;

        if (messages == null || messages.Length == 0)
        {
            return DefaultTrialSuccessMessagesFa[
                Random.Range(0, DefaultTrialSuccessMessagesFa.Length)
            ];
        }

        TitledMessageFa message = messages[Random.Range(0, messages.Length)];

        if (string.IsNullOrWhiteSpace(message.message))
        {
            return DefaultTrialSuccessMessagesFa[
                Random.Range(0, DefaultTrialSuccessMessagesFa.Length)
            ];
        }

        return message;
    }

    public string GetRandomLevelSuccessMessage()
    {
        if (levelSuccessMessagesFa == null || levelSuccessMessagesFa.Length == 0)
            return "مرحله را کامل رد کردی.";

        return levelSuccessMessagesFa[
            Random.Range(0, levelSuccessMessagesFa.Length)
        ];
    }

    public string GetRandomAssistedPassMessage()
    {
        if (assistedPassMessagesFa == null || assistedPassMessagesFa.Length == 0)
            return "این مرحله با کمک کامل شد.";

        return assistedPassMessagesFa[
            Random.Range(0, assistedPassMessagesFa.Length)
        ];
    }

    public string GetFinalSuccessMessage(LevelConfig level)
    {
        if (IsGatewayLevel(level))
        {
            return string.IsNullOrWhiteSpace(gatewaySuccessMessageFa)
                ? "عالی بود، مرحله کلیدی را رد کردی و کلید را گرفتی."
                : gatewaySuccessMessageFa;
        }

        return GetRandomLevelSuccessMessage();
    }

    // Compatibility methods for existing Treasure-game callers.
    public string GetSuccessMessage(LevelConfig level, int countOrSpan)
    {
        if (!string.IsNullOrWhiteSpace(level.successMessageFa))
        {
            return SafeFormat(
                level.successMessageFa,
                "مرحله را با موفقیت کامل کردی",
                countOrSpan
            );
        }

        return GetRandomTrialSuccess(level).message;
    }

    public string GetAssistedPassInfo(LevelConfig level)
    {
        if (!string.IsNullOrWhiteSpace(level.assistedPassInfoMessageFa))
            return level.assistedPassInfoMessageFa;

        return GetRandomAssistedPassMessage();
    }

    public string GetLevelUpInfo(LevelConfig level, int nextLevel)
    {
        return SafeFormat(
            level.levelUpInfoMessageFa,
            "ارتقا به مرحله {0}",
            nextLevel
        );
    }

    private static string SafeFormat(
        string pattern,
        string fallback,
        params object[] args
    )
    {
        string value = string.IsNullOrWhiteSpace(pattern)
            ? fallback
            : pattern;

        try
        {
            return string.Format(value, args);
        }
        catch
        {
            return string.Format(fallback, args);
        }
    }
}