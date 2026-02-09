using UnityEngine;

public enum ConstellationSpeedMode
{
    Relaxed,
    Standard,
    Challenge,
    Blitz
}

[CreateAssetMenu(menuName = "PandoraBox/Constellation/Timing Profile", fileName = "ConstellationTimingProfile")]
public class ConstellationTimingProfile : ScriptableObject
{
    [Header("Presentation")]
    [Tooltip("ms each star stays active during demo")]
    public int starDisplayMs = 1000;

    [Tooltip("ms gap between stars during demo")]
    public int gapMs = 250;

    [Header("Feedback")]
    public int successFeedbackMs = 1500;
    public int errorFeedbackMs = 2000;
    public int interTrialMs = 2000;

    [Header("Input safeguards")]
    [Tooltip("ms cooldown between taps to prevent accidental multi taps")]
    public int tapCooldownMs = 100;

    public float StarDisplaySeconds => starDisplayMs / 1000f;
    public float GapSeconds => gapMs / 1000f;
    public float SuccessFeedbackSeconds => successFeedbackMs / 1000f;
    public float ErrorFeedbackSeconds => errorFeedbackMs / 1000f;
    public float InterTrialSeconds => interTrialMs / 1000f;
    public float TapCooldownSeconds => tapCooldownMs / 1000f;
}
