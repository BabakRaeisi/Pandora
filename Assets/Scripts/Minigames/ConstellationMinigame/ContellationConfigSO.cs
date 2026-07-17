using System;
using UnityEngine;

[CreateAssetMenu(menuName = "PandoraBox/Constellation/Config", fileName = "ConstellationConfig")]
public class ConstellationConfigSO : ScriptableObject
{
    [System.Serializable]
    public struct TitledMessageFa
    {
        public string title;
        [TextArea] public string message;
    }

    [System.Serializable]
    public class LevelConfig
    {
        [Range(1, 16)] public int levelNumber = 1;
        [Min(1)] public int trials = 5;

        [Header("Progression")]
        public bool isGatewayLevel = false;

        [Header("Difficulty (span = sequence length)")]
        [Range(2, 7)] public int spanMin = 2;
        [Range(2, 7)] public int spanMax = 3;

        [Header("Level Lock (applies before entering this level)")]
        [Min(0f)] public float lockDurationHours = 0f;

        [Header("Wrong Pattern (FA)")]
        public TitledMessageFa[] wrongPatternMessagesFa;

        [Header("Trial Success (FA)")]
        public string successTitleFa = "آفرین";
        public TitledMessageFa[] trialSuccessMessagesFa;
    }

    public LevelConfig[] levels;

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
        "این مرحله را با تمرکز و دقت پشت سر گذاشتی.",
        "کار تو خیلی خوب بود،برو برای مرحله بعد.",
        "این مرحله را کامل و موفق تمام کردی."
    };

    [Header("Assisted Pass Messages (shared)")]
    public string[] assistedPassMessagesFa = new string[]
    {
        "این مرحله با کمک کامل شد،ادامه بده.",
        "با کمک از این مرحله هم عبور کردی.",
        "مرحله برایت کامل شد،تو داری خوب جلو می‌روی.",
        "با هم از این مرحله هم رد شدیم.",
        "این مرحله با کمک تمام شد،تو مسیرت را ادامه بده.",
        "این مرحله با کمک رد شد،مرحله بعدی آماده است."
    };

    [Header("Gateway Success Message (shared)")]
    [TextArea] public string gatewaySuccessMessageFa =
        "عالی بود،مرحله کلیدی را رد کردی و کلید را گرفتی.";

    private void OnValidate()
    {
        if (levels == null) return;

        for (int i = 0; i < levels.Length; i++)
        {
            var level = levels[i];
            if (level == null) continue;

            if (level.wrongPatternMessagesFa == null || level.wrongPatternMessagesFa.Length == 0)
                level.wrongPatternMessagesFa = (TitledMessageFa[])DefaultWrongPatternMessagesFa.Clone();

            if (level.trialSuccessMessagesFa == null || level.trialSuccessMessagesFa.Length == 0)
                level.trialSuccessMessagesFa = (TitledMessageFa[])DefaultTrialSuccessMessagesFa.Clone();
        }

        if (levelSuccessMessagesFa == null || levelSuccessMessagesFa.Length == 0)
            levelSuccessMessagesFa = new string[]
            {
                "عالی بود،این مرحله را خیلی خوب رد کردی.",
                "مرحله تمام شد و تو خیلی خوب پیش رفتی.",
                "با موفقیت از این مرحله عبور کردی.",
                "این مرحله را با تمرکز و دقت پشت سر گذاشتی.",
                "کار تو خیلی خوب بود،برو برای مرحله بعد.",
                "این مرحله را کامل و موفق تمام کردی."
            };

        if (assistedPassMessagesFa == null || assistedPassMessagesFa.Length == 0)
            assistedPassMessagesFa = new string[]
            {
                "این مرحله با کمک کامل شد،ادامه بده.",
                "با کمک از این مرحله هم عبور کردی.",
                "مرحله برایت کامل شد،تو داری خوب جلو می‌روی.",
                "با هم از این مرحله هم رد شدیم.",
                "این مرحله با کمک تمام شد،تو مسیرت رو ادامه بده.",
                "این مرحله با کمک رد شد،مرحله بعدی آماده است."
            };
    }

    public LevelConfig GetLevel(int levelNumber)
    {
        if (levels == null)
        {
            Debug.LogWarning("[ConstellationConfigSO] levels is NULL");
            return null;
        }

        levelNumber = Mathf.Clamp(levelNumber, 1, 16);

        for (int i = 0; i < levels.Length; i++)
        {
            if (levels[i] != null && levels[i].levelNumber == levelNumber)
            {
                Debug.Log($"[ConstellationConfigSO] GetLevel({levelNumber}) => index={i}, levelNumber={levels[i].levelNumber}, isGateway={levels[i].isGatewayLevel}");
                return levels[i];
            }
        }

        Debug.LogWarning($"[ConstellationConfigSO] GetLevel({levelNumber}) => NOT FOUND");
        return null;
    }

    public bool IsGatewayLevel(LevelConfig level) => level != null && level.isGatewayLevel;

    public string GetSuccessTitle(LevelConfig level) =>
        string.IsNullOrWhiteSpace(level?.successTitleFa) ? "آفرین" : level.successTitleFa;

    public TitledMessageFa GetRandomWrongPattern(LevelConfig level)
    {
        TitledMessageFa[] arr = level?.wrongPatternMessagesFa;

        if (arr == null || arr.Length == 0)
        {
            int fallbackIdx = UnityEngine.Random.Range(0, DefaultWrongPatternMessagesFa.Length);
            return DefaultWrongPatternMessagesFa[fallbackIdx];
        }

        int idx = UnityEngine.Random.Range(0, arr.Length);
        TitledMessageFa item = arr[idx];

        if (string.IsNullOrWhiteSpace(item.message))
        {
            int fallbackIdx = UnityEngine.Random.Range(0, DefaultWrongPatternMessagesFa.Length);
            return DefaultWrongPatternMessagesFa[fallbackIdx];
        }

        return item;
    }

    public TitledMessageFa GetRandomTrialSuccess(LevelConfig level)
    {
        var arr = level?.trialSuccessMessagesFa;
        if (arr == null || arr.Length == 0)
            return new TitledMessageFa { title = "آفرین", message = "عالی بود،همین‌طور ادامه بده." };

        int idx = UnityEngine.Random.Range(0, arr.Length);
        return arr[idx];
    }

    // Keep compatibility with existing calls
    public string GetTrialSuccessMessage(LevelConfig level, int trialNumber, int span) =>
        GetRandomTrialSuccess(level).message;

    public string GetWrongPatternMessage(LevelConfig level) =>
        GetRandomWrongPattern(level).message;

    public string GetRandomLevelSuccessMessage()
    {
        if (levelSuccessMessagesFa == null || levelSuccessMessagesFa.Length == 0)
            return "مرحله را کامل رد کردی.";

        int idx = UnityEngine.Random.Range(0, levelSuccessMessagesFa.Length);
        return levelSuccessMessagesFa[idx];
    }

    public string GetRandomAssistedPassMessage()
    {
        if (assistedPassMessagesFa == null || assistedPassMessagesFa.Length == 0)
            return "این مرحله با کمک کامل شد.";

        int idx = UnityEngine.Random.Range(0, assistedPassMessagesFa.Length);
        return assistedPassMessagesFa[idx];
    }

    public string GetFinalSuccessMessage(LevelConfig level)
    {
        if (IsGatewayLevel(level))
            return string.IsNullOrWhiteSpace(gatewaySuccessMessageFa)
                ? "عالی بود،مرحله کلیدی را رد کردی و کلید را گرفتی."
                : gatewaySuccessMessageFa;

        return GetRandomLevelSuccessMessage();
    }
}
