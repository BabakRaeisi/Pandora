// // BridgeGameManager.cs
// using System;
// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using TMPro;
// using RTLTMPro;

// public class BridgeGameManager : MonoBehaviour
// {
//     [Header("Refs")]
//     [SerializeField] private SWMHUD hud;
//     [SerializeField] private BridgeConfig config;
//     [SerializeField] private SessionDataSO sessionData;

//     [Header("Countdown")]
//     [SerializeField] private RTLTextMeshPro countdownText;

//     [Header("UI Layout (Anchors)")]
//     [SerializeField] private RectTransform playArea;
//     [SerializeField] private RectTransform topChasmAnchor;
//     [SerializeField] private RectTransform bottomChasmAnchor;

//     [Header("Pieces (IDs 0..11, 2 columns)")]
//     [SerializeField] private List<BridgePieceUI> pieces = new();

//     [Header("Board")]
//     [SerializeField] private int cols = 2;
//     [SerializeField] private int totalRows = 6;

//     [Header("Placement")]
//     [SerializeField] private float columnGap = 260f;
//     [SerializeField] private float staggerX = 35f;

//     [Header("Direction")]
//     [SerializeField] private bool randomizeBottomToTop = true;
//     [SerializeField, Range(0f, 1f)] private float bottomToTopChance = 0.5f;

//     // ── Current level state ───────────────────────────────────────────────────
//     private int currentLevel;
//     private BridgeConfig.LevelConfig levelCfg;

//     // ── Trial state ───────────────────────────────────────────────────────────
//     private int trialsCompleteInLevel;
//     private int consecutiveFailsOnLevel;
//     private int trialIndexInLevel;

//     private readonly Dictionary<int, BridgePieceUI> piecesById = new();

//     private int activeRows;
//     private int goalPieces;
//     private List<int> targetSequence = new();

//     private float levelStartTime;
//     private float trialStartTime;
//     private bool trialComplete;
//     private bool inputEnabled;
//     private int builtCount;
//     private int wrongAttempts;

//     private void Awake()
//     {
//         if (pieces == null || pieces.Count == 0)
//             pieces = new List<BridgePieceUI>(GetComponentsInChildren<BridgePieceUI>(true));

//         piecesById.Clear();

//         foreach (var p in pieces)
//         {
//             if (!p) continue;

//             if (piecesById.ContainsKey(p.Id))
//             {
//                 Debug.LogError($"BridgeGameManager: Duplicate Id={p.Id} on '{p.name}'.");
//                 continue;
//             }

//             piecesById.Add(p.Id, p);

//             int r = p.Id / cols;
//             int c = p.Id % cols;
//             p.SetGrid(r, c);

//             p.Clicked -= OnPiecePressed;
//             p.Clicked += OnPiecePressed;
//         }
//     }

//     private void OnDestroy()
//     {
//         foreach (var kv in piecesById)
//             if (kv.Value) kv.Value.Clicked -= OnPiecePressed;
//     }

//     private void Start()
//     {
//         AudioManager.Instance.StopAll();
//         AudioManager.Instance.Play("BridgeAmbient");

//         StartCoroutine(BeginGameAfterCountdown());
//     }

//     private IEnumerator BeginGameAfterCountdown()
//     {
//         if (countdownText != null)
//         {
//             countdownText.gameObject.SetActive(true);
//             countdownText.text = "3";
//             yield return new WaitForSeconds(1f);
//             countdownText.text = "2";
//             yield return new WaitForSeconds(1f);
//             countdownText.text = "1";
//             yield return new WaitForSeconds(1f);
//             countdownText.gameObject.SetActive(false);
//         }

//         var data = PlayerDataManager.Instance.Data;
//         StartLevel(data.bridgeLevel);
//     }

//     public void StartLevel(int levelNumber)
//     {
//         currentLevel = Mathf.Clamp(levelNumber, 1, ProgressionManager.MAX_LEVEL);

//         if (!config || !sessionData)
//         {
//             Debug.LogError("BridgeGameManager: Missing config or sessionData.");
//             return;
//         }

//         levelCfg = config.GetLevel(currentLevel);
//         if (levelCfg.levelNumber == 0)
//         {
//             Debug.LogError($"[Bridge] No LevelConfig for level {currentLevel}");
//             return;
//         }

//         levelStartTime = Time.time;
//         trialsCompleteInLevel = 0;
//         consecutiveFailsOnLevel = 0;
//         trialIndexInLevel = 0;

//         hud?.SetupDay(levelCfg.trials);
//         hud?.SetTrialsDone(0);

//         StartNextTrial();
//     }

//     public void StartNextTrial()
//     {
//         if (trialsCompleteInLevel >= levelCfg.trials)
//         {
//             CompleteLevelAfterTrials();
//             return;
//         }

//         StopAllCoroutines();

//         trialComplete = false;
//         inputEnabled = false;
//         builtCount = 0;
//         wrongAttempts = 0;

//         activeRows = Mathf.Clamp(levelCfg.minPieces, 2, totalRows);

//         if (levelCfg.pattern == BridgePattern.ZigZag)
//         {
//             goalPieces = Mathf.Clamp(levelCfg.maxPieces, activeRows + 1, activeRows * 2);
//         }
//         else
//         {
//             int minLen = Mathf.Max(levelCfg.minPieces, activeRows);
//             int maxLen = Mathf.Min(levelCfg.maxPieces, activeRows * 2);
//             goalPieces = UnityEngine.Random.Range(minLen, maxLen + 1);
//         }

//         ApplyActiveSpan(activeRows);
//         LayoutActiveSpanConnectingChasms(activeRows);

//         bool startFromBottom = randomizeBottomToTop
//             ? (UnityEngine.Random.value < bottomToTopChance)
//             : false;

//         bool forceSwitch = (levelCfg.pattern == BridgePattern.ZigZag);

//         targetSequence = BridgePathGenerator.Generate2ColPath(
//             activeRows,
//             goalPieces,
//             startFromBottom,
//             forceSwitch,
//             3000
//         );

//         if (targetSequence == null || targetSequence.Count == 0)
//         {
//             Debug.LogError($"[Bridge] Failed to generate path level={currentLevel}");
//             return;
//         }

//         hud?.SetupTrial(goalPieces);
//         hud?.SetCollectedFound(0);

//         ResetActivePiecesToIdle();
//         SetInputEnabled(false);

//         StartCoroutine(PresentThenConstruct());
//     }

//     private void ApplyActiveSpan(int spanRows)
//     {
//         foreach (var kv in piecesById)
//         {
//             int id = kv.Key;
//             var piece = kv.Value;
//             if (!piece) continue;

//             int r = id / cols;
//             piece.gameObject.SetActive(r >= 0 && r < spanRows);
//         }
//     }

//     private void LayoutActiveSpanConnectingChasms(int spanRows)
//     {
//         if (!playArea) return;

//         float leftX = -(columnGap * 0.5f);
//         float rightX = (columnGap * 0.5f);

//         float topY, bottomY;

//         if (topChasmAnchor && bottomChasmAnchor)
//         {
//             Vector2 localTop, localBottom;

//             RectTransformUtility.ScreenPointToLocalPointInRectangle(
//                 playArea,
//                 RectTransformUtility.WorldToScreenPoint(null, topChasmAnchor.position),
//                 null,
//                 out localTop
//             );

//             RectTransformUtility.ScreenPointToLocalPointInRectangle(
//                 playArea,
//                 RectTransformUtility.WorldToScreenPoint(null, bottomChasmAnchor.position),
//                 null,
//                 out localBottom
//             );

//             topY = localTop.y;
//             bottomY = localBottom.y;

//             if (topY < bottomY) (topY, bottomY) = (bottomY, topY);
//         }
//         else
//         {
//             var pr = playArea.rect;
//             topY = pr.height * 0.5f - 140f;
//             bottomY = -pr.height * 0.5f + 140f;
//         }

//         float spanH = Mathf.Max(10f, topY - bottomY);
//         float rowStep = (spanRows <= 1) ? 0f : (spanH / (spanRows - 1));

//         for (int id = 0; id < totalRows * cols; id++)
//         {
//             if (!piecesById.TryGetValue(id, out var piece) || !piece) continue;
//             if (!piece.gameObject.activeInHierarchy) continue;

//             int r = id / cols;
//             int c = id % cols;
//             if (r < 0 || r >= spanRows) continue;

//             RectTransform rt = piece.GetComponent<RectTransform>();
//             if (!rt) continue;

//             if (rt.parent != playArea)
//                 rt.SetParent(playArea, false);

//             rt.anchorMin = new Vector2(0.5f, 0.5f);
//             rt.anchorMax = new Vector2(0.5f, 0.5f);
//             rt.pivot = new Vector2(0.5f, 0.5f);

//             float y = topY - (r * rowStep);
//             float x = (c == 0) ? leftX : rightX;
//             x += ((r % 2) == 0) ? -staggerX : staggerX;

//             rt.anchoredPosition = new Vector2(x, y);
//         }
//     }

//     private IEnumerator PresentThenConstruct()
//     {
//         foreach (var id in targetSequence)
//         {
//             if (!piecesById.TryGetValue(id, out var piece) || !piece) continue;
//             if (!piece.gameObject.activeInHierarchy) continue;

//             piece.SetState(BridgePieceState.Highlighted);
//             yield return new WaitForSeconds(levelCfg.displayMs / 1000f);
//             piece.SetState(BridgePieceState.Idle);
//             yield return new WaitForSeconds(levelCfg.gapMs / 1000f);
//         }

//         trialStartTime = Time.time;
//         inputEnabled = true;
//         SetInputEnabled(true);
//     }

//     private void OnPiecePressed(BridgePieceUI piece)
//     {
//         if (!inputEnabled) return;
//         if (trialComplete || !piece) return;
//         if (!piece.gameObject.activeInHierarchy) return;
//         if (builtCount < 0 || builtCount >= targetSequence.Count) return;

//         int expectedId = targetSequence[builtCount];

//         if (piece.Id == expectedId)
//         {
//             builtCount++;
//             piece.SetState(BridgePieceState.Built);
//             hud?.SetCollectedFound(builtCount);
//             AudioManager.Instance.Play("StoneCorrectStep");
//             if (builtCount >= goalPieces)
//                 CompleteTrial();
//         }
//         else
//         {
//             wrongAttempts++;
//             piece.FlashError();
//             hud?.AddErrorAndWarn();
//             AudioManager.Instance.Play("StepStoneWrong");
//         }
//     }

//     private void CompleteTrial()
//     {
//         trialComplete = true;
//         inputEnabled = false;
//         SetInputEnabled(false);

//         int completionMs = Mathf.RoundToInt((Time.time - trialStartTime) * 1000f);

//         // Evaluate this trial for scoring
//         var trialResult = ProgressionManager.Instance.EvaluateTrial(
//             "Bridge",
//             isCorrect: true,
//             wrongAttempts: wrongAttempts,
//             completionTimeMs: completionMs,
//             span: goalPieces,
//             consecutiveFails: consecutiveFailsOnLevel
//         );

//         // Record trial
//         RecordTrial("Bridge", trialResult, true, wrongAttempts, completionMs);

//         trialsCompleteInLevel++;
//         hud?.SetTrialsDone(trialsCompleteInLevel);

//         if (trialsCompleteInLevel >= levelCfg.trials)
//         {
//             CompleteLevelAfterTrials();
//         }
//         else
//         {
//             hud?.ShowTrialComplete();
//         }
//     }

//     void CompleteLevelAfterTrials()
//     {
//         // Compute aggregate level performance
//         int levelCompletionMs = Mathf.RoundToInt((Time.time - levelStartTime) * 1000f);
//         float avgSpan = (levelCfg.minPieces + levelCfg.maxPieces) * 0.5f;

//         var levelResult = ProgressionManager.Instance.EvaluateTrial(
//             "Bridge",
//             isCorrect: true,
//             wrongAttempts: consecutiveFailsOnLevel,
//             completionTimeMs: levelCompletionMs,
//             span: Mathf.RoundToInt(avgSpan),
//             consecutiveFails: 0
//         );

//         // Commit the level completion
//         var finalResult = ProgressionManager.Instance.CompleteLevel("Bridge", levelResult);

//         Debug.Log($"[Bridge] Level {currentLevel} completed. Score: {finalResult.score:F1}, Stars: {finalResult.stars}. " +
//                   $"Cap: {finalResult.levelCapReached}, SWM unlocked: {finalResult.nextMinigameUnlocked}");

//         hud?.ShowDayComplete();

//         if (finalResult.levelCapReached)
//         {
//             Debug.Log("[Bridge] Session cap reached. No more levels available this session.");
//         }

//         if (finalResult.nextMinigameUnlocked)
//         {
//             Debug.Log("[Bridge] SWM minigame is now unlocked!");
//         }

//         if (finalResult.programCompletable)
//         {
//             Debug.Log("[Bridge] Program is now completable! (All three minigames at gate level.)");
//         }
//     }

//     void RecordTrial(string minigameId, ProgressionManager.LevelResult result, bool isCorrect, int wrongAttempts, int completionMs)
//     {
//         sessionData.Add(new TrialRecord
//         {
//             minigame_id = minigameId,
//             day = currentLevel,           // Legacy: store level as day
//             level_number = currentLevel,
//             trial_index = trialIndexInLevel + 1,

//             span = goalPieces,
//             target_sequence = new List<int>(targetSequence),
//             sequence_recalled = new List<int>(),  // Bridge doesn't track player input sequence

//             is_correct = isCorrect,
//             wrong_attempts = wrongAttempts,
//             completion_time_ms = completionMs,

//             level_score = result.score,
//             stars = result.stars,
//             passed = result.passed,
//             strong_pass = result.strongPass,
//             assisted_pass = result.assistedPass,
//             consecutive_fails = consecutiveFailsOnLevel,

//             timestamp_iso = DateTime.UtcNow.ToString("o")
//         });

//         trialIndexInLevel++;
//     }

//     private void ResetActivePiecesToIdle()
//     {
//         foreach (var kv in piecesById)
//         {
//             var p = kv.Value;
//             if (!p) continue;
//             if (!p.gameObject.activeInHierarchy) continue;
//             p.SetState(BridgePieceState.Idle);
//         }
//     }

//     private void SetInputEnabled(bool enabled)
//     {
//         foreach (var kv in piecesById)
//         {
//             var p = kv.Value;
//             if (!p) continue;
//             if (!p.gameObject.activeInHierarchy) continue;
//             p.SetInteractable(enabled);
//         }
//     }
// }
