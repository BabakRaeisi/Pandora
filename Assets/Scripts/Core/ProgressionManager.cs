using UnityEngine;

/// <summary>
/// Central singleton that owns all progression rules for the three minigames.
///
/// USAGE (from a game manager, once a level is finished):
///
///   var result = ProgressionManager.Instance.EvaluateTrial(
///       "Constellation", isCorrect, wrongAttempts, completionTimeMs, span, consecutiveFails);
///
///   if (result.passed)
///       result = ProgressionManager.Instance.CompleteLevel("Constellation", result);
///
///   // then read result.stars / result.levelCapReached / result.nextMinigameUnlocked etc.
/// </summary>
public class ProgressionManager : MonoBehaviour
{
    public static ProgressionManager Instance { get; private set; }

    // ── Tuning constants ──────────────────────────────────────────────────────
    /// <summary>Total number of levels per minigame.</summary>
    public const int MAX_LEVEL = 16;

    /// <summary>
    /// How many levels Bridge / SWM allow per session by default.
    /// Constellation has no hard cap — it is always the entry point.
    /// </summary>
    public const int SESSION_LEVEL_CAP = 2;

    /// <summary>Consecutive fails on the same level before an assisted pass fires.</summary>
    public const int ASSISTED_PASS_FAIL_LIMIT = 4;

    /// <summary>Expected seconds per span unit, used for speed scoring.</summary>
    public const float EXPECTED_TIME_PER_SPAN_SEC = 2f;

    // Score weights (must sum to 1.0)
    private const float W_ACCURACY = 0.60f;
    private const float W_SPEED    = 0.25f;
    private const float W_ERROR    = 0.15f;

    // Star thresholds
    private const float STAR3_MIN = 85f;
    private const float STAR2_MIN = 65f;
    private const float STAR1_MIN = 40f;

    // ── Result returned to game managers ──────────────────────────────────────
    public struct LevelResult
    {
        /// <summary>0 – 100 composite score.</summary>
        public float score;

        /// <summary>0 – 3 stars.</summary>
        public int stars;

        /// <summary>True when stars >= 1 OR assisted pass fired.</summary>
        public bool passed;

        /// <summary>Passed with zero wrong attempts AND within expected time.</summary>
        public bool strongPass;

        /// <summary>System granted pass after hitting the consecutive-fail limit.</summary>
        public bool assistedPass;

        /// <summary>
        /// Session level cap reached for this minigame (Bridge / SWM only).
        /// Game manager should prevent starting another level until next session.
        /// </summary>
        public bool levelCapReached;

        /// <summary>Bridge or SWM was just unlocked by completing gate level.</summary>
        public bool nextMinigameUnlocked;

        /// <summary>All three minigames have hit gate level — program is completable.</summary>
        public bool programCompletable;
    }

    // ─────────────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PUBLIC API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Calculates score / stars and evaluates pass/strong/assisted state for one
    /// level attempt.  Does NOT write to PlayerSaveData — call CompleteLevel()
    /// only if you want to commit a passed result.
    /// </summary>
    /// <param name="minigameId">"Constellation", "Bridge", or "SWM"</param>
    /// <param name="isCorrect">Whether the full sequence was recalled correctly.</param>
    /// <param name="wrongAttempts">Total wrong inputs during this trial.</param>
    /// <param name="completionTimeMs">Time from stimulus-end to last input, in ms.</param>
    /// <param name="span">Sequence length for this level.</param>
    /// <param name="consecutiveFails">
    /// How many times in a row the player has failed THIS level (before this attempt).
    /// </param>
    public LevelResult EvaluateTrial(
        string minigameId,
        bool   isCorrect,
        int    wrongAttempts,
        int    completionTimeMs,
        int    span,
        int    consecutiveFails)
    {
        float score      = CalculateScore(isCorrect, wrongAttempts, completionTimeMs, span);
        int   stars      = ScoreToStars(score);
        bool  passed     = stars >= 1;

        float expectedMs = span * EXPECTED_TIME_PER_SPAN_SEC * 1000f;
        bool  strongPass = passed && wrongAttempts == 0 && completionTimeMs <= expectedMs;

        // Assisted pass overrides failure when player is stuck
        bool assistedPass = !passed && consecutiveFails >= ASSISTED_PASS_FAIL_LIMIT;
        if (assistedPass) passed = true;

        return new LevelResult
        {
            score        = score,
            stars        = stars,
            passed       = passed,
            strongPass   = strongPass,
            assistedPass = assistedPass,
        };
    }

    /// <summary>
    /// Commits a completed level to PlayerSaveData and returns an enriched
    /// LevelResult with gate / unlock / cap flags.
    /// Must be called exactly once per level, after EvaluateTrial confirms passed == true.
    /// </summary>
    /// <param name="minigameId">"Constellation", "Bridge", or "SWM"</param>
    /// <param name="result">The result returned by EvaluateTrial.</param>
    public LevelResult CompleteLevel(string minigameId, LevelResult result)
    {
        var data = PlayerDataManager.Instance.Data;

        // ── Increment counters ────────────────────────────────────────────────
        IncrementLevel(minigameId, data);
        IncrementSessionCounter(minigameId, data);

        int sessionPlayed = GetSessionCount(minigameId, data);
        int newLevel      = GetLevel(minigameId, data);

        // ── Bonus slot management (Bridge / SWM only) ─────────────────────────
        // A strong pass on a base-cap level (not the bonus level itself) earns
        // one extra level slot for this session.
        if (minigameId != "Constellation")
        {
            if (result.strongPass && sessionPlayed <= SESSION_LEVEL_CAP)
                SetBonusAvailable(minigameId, data, true);

            // Consume the bonus once the player has played beyond the base cap
            if (sessionPlayed > SESSION_LEVEL_CAP)
                SetBonusAvailable(minigameId, data, false);
        }

        // ── Gate and perfect tiers ────────────────────────────────────────────
        // Level is stored as "next level to play", so newLevel == GATE_LEVEL + 1
        // means the player just finished level GATE_LEVEL.
        if (newLevel > MAX_LEVEL)
            SetPerfect(minigameId, data);

        // ── Unlock next minigame if gate was just reached ─────────────────────
        bool justUnlocked = false;

        if (minigameId == "Constellation" && !data.bridgeUnlocked && data.constellationGateReached)
        {
            data.bridgeUnlocked = true;
            justUnlocked = true;
        }
        else if (minigameId == "Bridge" && !data.swmUnlocked && data.bridgeGateReached)
        {
            data.swmUnlocked = true;
            justUnlocked = true;
        }

        // ── Programme becomes completable when all three hit the gate ─────────
        bool completable = data.constellationGateReached
                        && data.bridgeGateReached
                        && data.swmGateReached;

        if (completable && !data.programCompleted)
        {
            // Mark as completable but do NOT set programCompleted — that is a
            // user-triggered action (Phase 7 / DayCompletionManager).
        }

        // ── Persist ───────────────────────────────────────────────────────────
        PlayerDataManager.Instance.Save();

        // ── Populate and return ───────────────────────────────────────────────
        result.levelCapReached      = IsSessionCapReached(minigameId, data);
        result.nextMinigameUnlocked = justUnlocked;
        result.programCompletable   = completable;
        return result;
    }

    /// <summary>
    /// Returns true when the player has exhausted their level slots for this
    /// minigame in the current session.
    /// Always returns false for Constellation (no cap).
    /// </summary>
    public bool IsSessionCapReached(string minigameId, PlayerSaveData data = null)
    {
        if (minigameId == "Constellation") return false;

        data ??= PlayerDataManager.Instance.Data;

        int  played        = GetSessionCount(minigameId, data);
        bool bonusAvailable = GetBonusAvailable(minigameId, data);
        int  cap           = SESSION_LEVEL_CAP + (bonusAvailable ? 1 : 0);

        return played >= cap;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SCORE HELPERS  (static — usable without an instance for testing)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Computes a 0–100 score.
    ///   60 % Accuracy  – full correct = 1.0, incorrect = 0.33 (effort credit)
    ///   25 % Speed     – within expected time = 1.0,
    ///                    within 1.5x expected = 0.5, over = 0.0
    ///   15 % Error control – (span − wrongAttempts) / span, clamped 0–1
    /// </summary>
    public static float CalculateScore(bool isCorrect, int wrongAttempts, int completionTimeMs, int span)
    {
        float accuracy = isCorrect ? 1f : 0.33f;

        float errorControl = span > 0
            ? Mathf.Clamp01((float)(span - wrongAttempts) / span)
            : 1f;

        float expectedMs = span * EXPECTED_TIME_PER_SPAN_SEC * 1000f;
        float speed;
        if (completionTimeMs <= expectedMs)
            speed = 1f;
        else if (completionTimeMs <= expectedMs * 1.5f)
            speed = 0.5f;
        else
            speed = 0f;

        return Mathf.Clamp(
            100f * (W_ACCURACY * accuracy + W_SPEED * speed + W_ERROR * errorControl),
            0f, 100f);
    }

    /// <summary>Maps a 0–100 score to 0–3 stars.</summary>
    public static int ScoreToStars(float score)
    {
        if (score >= STAR3_MIN) return 3;
        if (score >= STAR2_MIN) return 2;
        if (score >= STAR1_MIN) return 1;
        return 0;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PRIVATE HELPERS
    // ─────────────────────────────────────────────────────────────────────────

    static void IncrementLevel(string id, PlayerSaveData d)
    {
        if      (id == "Constellation") d.constellationLevel = Mathf.Min(d.constellationLevel + 1, MAX_LEVEL + 1);
        else if (id == "Bridge")        d.bridgeLevel        = Mathf.Min(d.bridgeLevel        + 1, MAX_LEVEL + 1);
        else                            d.swmLevel           = Mathf.Min(d.swmLevel           + 1, MAX_LEVEL + 1);
    }

    static void IncrementSessionCounter(string id, PlayerSaveData d)
    {
        if      (id == "Constellation") d.constellationLevelsPlayedThisSession++;
        else if (id == "Bridge")        d.bridgeLevelsPlayedThisSession++;
        else                            d.swmLevelsPlayedThisSession++;
    }

    static int GetLevel(string id, PlayerSaveData d)
    {
        if (id == "Constellation") return d.constellationLevel;
        if (id == "Bridge")        return d.bridgeLevel;
        return d.swmLevel;
    }

    static int GetSessionCount(string id, PlayerSaveData d)
    {
        if (id == "Constellation") return d.constellationLevelsPlayedThisSession;
        if (id == "Bridge")        return d.bridgeLevelsPlayedThisSession;
        return d.swmLevelsPlayedThisSession;
    }

    static void SetPerfect(string id, PlayerSaveData d)
    {
        if      (id == "Constellation") d.constellationPerfect = true;
        else if (id == "Bridge")        d.bridgePerfect        = true;
        else                            d.swmPerfect           = true;
    }

    static void SetBonusAvailable(string id, PlayerSaveData d, bool value)
    {
        if      (id == "Bridge") d.bonusLevelAvailableBridge = value;
        else if (id == "SWM")    d.bonusLevelAvailableSwm   = value;
    }

    static bool GetBonusAvailable(string id, PlayerSaveData d)
    {
        if (id == "Bridge") return d.bonusLevelAvailableBridge;
        if (id == "SWM")    return d.bonusLevelAvailableSwm;
        return false;
    }
}
