using System;
using UnityEngine;

[CreateAssetMenu(menuName = "PandoraBox/Constellation/Config", fileName = "ConstellationConfig")]
public class ConstellationConfigSO : ScriptableObject
{
    [Serializable]
    public struct ToastMessageFa
    {
        public string title;
        [TextArea] public string message;
    }

    [System.Serializable]
    public class LevelConfig
    {
        [Range(1, 16)] public int levelNumber = 1;
        [Min(1)] public int trials = 5;

        [Header("Difficulty (span = sequence length)")]
        [Range(2, 7)] public int spanMin = 2;
        [Range(2, 7)] public int spanMax = 3;

        [Header("Level Lock (applies before entering this level)")]
        [Min(0f)] public float lockDurationHours = 0f;

        [Header("Trial Success (FA)")]
        public string successTitleFa = "آفرین قهرمان!";
        public string[] trialSuccessMessagesFa = new string[]
        {
            "عالی بود! همین‌طور ادامه بده ✨",
            "درست زدی! ستاره‌ها باهات هماهنگن 🌟",
            "خیلی خوب پیش می‌ری! 👏"
        };

        [Header("Level Complete Success (FA) - title + message array")]
        public ToastMessageFa[] levelSuccessMessagesFa = new ToastMessageFa[]
        {
            new ToastMessageFa { title = "درخشان بودی!", message = "این مرحله رو قشنگ و کامل رد کردی. بزن بریم مرحله بعد! 🚀" },
            new ToastMessageFa { title = "قهرمان آسمون!", message = "مرحله تموم شد و عالی بازی کردی. ادامه بده که خیلی خوب پیش می‌ری 🌌" },
            new ToastMessageFa { title = "آفرین!", message = "یه مرحله دیگه هم با موفقیت پشت سر گذاشتی. خیلی خوبه! ⭐" }
        };

        [Header("Assisted Pass (FA) - title + message array")]
        public ToastMessageFa[] assistedPassMessagesFa = new ToastMessageFa[]
        {
            new ToastMessageFa { title = "کنارت هستیم 💜", message = "این مرحله برات کامل شد. خیلی خوبه که ادامه می‌دی!" },
            new ToastMessageFa { title = "عالیه که تلاش می‌کنی", message = "ما کمک کردیم این مرحله رد بشه. با هم می‌ریم جلو 🌟" },
            new ToastMessageFa { title = "قدم‌به‌قدم جلو می‌ریم", message = "مرحله با کمک رد شد؛ تو فقط ادامه بده، کارت عالیه 👏" }
        };
    }

    public LevelConfig[] levels;

    public LevelConfig GetLevel(int levelNumber)
    {
        if (levels == null) return null;
        levelNumber = Mathf.Clamp(levelNumber, 1, 16);

        for (int i = 0; i < levels.Length; i++)
            if (levels[i] != null && levels[i].levelNumber == levelNumber)
                return levels[i];

        return null;
    }

    public string GetSuccessTitle(LevelConfig level) =>
        string.IsNullOrWhiteSpace(level?.successTitleFa) ? "آفرین قهرمان!" : level.successTitleFa;

    public string GetTrialSuccessMessage(LevelConfig level, int trialNumber, int span)
    {
        if (level?.trialSuccessMessagesFa == null || level.trialSuccessMessagesFa.Length == 0)
            return "عالی بود! همین‌طور ادامه بده ✨";

        int idx = Mathf.Clamp(trialNumber - 1, 0, level.trialSuccessMessagesFa.Length - 1);
        return SafeFormat(level.trialSuccessMessagesFa[idx], "عالی بود! همین‌طور ادامه بده ✨", trialNumber, span);
    }

    public ToastMessageFa GetLevelSuccessToast(LevelConfig level, int levelNumber)
    {
        var arr = level?.levelSuccessMessagesFa;
        if (arr == null || arr.Length == 0)
            return new ToastMessageFa { title = "درخشان بودی!", message = "این مرحله رو کامل رد کردی. بزن بریم مرحله بعد! 🚀" };

        int idx = Mathf.Abs(levelNumber - 1) % arr.Length;
        return Normalize(arr[idx], "درخشان بودی!", "این مرحله رو کامل رد کردی. بزن بریم مرحله بعد! 🚀");
    }

    public ToastMessageFa GetAssistedPassToast(LevelConfig level, int levelNumber)
    {
        var arr = level?.assistedPassMessagesFa;
        if (arr == null || arr.Length == 0)
            return new ToastMessageFa { title = "کنارت هستیم 💜", message = "این مرحله برات کامل شد. خیلی خوبه که ادامه می‌دی!" };

        int idx = Mathf.Abs(levelNumber - 1) % arr.Length;
        return Normalize(arr[idx], "کنارت هستیم 💜", "این مرحله برات کامل شد. خیلی خوبه که ادامه می‌دی!");
    }

    private static ToastMessageFa Normalize(ToastMessageFa item, string fallbackTitle, string fallbackMessage)
    {
        if (string.IsNullOrWhiteSpace(item.title)) item.title = fallbackTitle;
        if (string.IsNullOrWhiteSpace(item.message)) item.message = fallbackMessage;
        return item;
    }

    private static string SafeFormat(string pattern, string fallback, params object[] args)
    {
        var p = string.IsNullOrWhiteSpace(pattern) ? fallback : pattern;
        try { return string.Format(p, args); }
        catch { return fallback; }
    }
}
