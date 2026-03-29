using System;
using System.Collections.Generic;

[Serializable]
public class SessionUploadRequest
{
    public PlayerProfile profile;
    public PlayerSaveData saveData;
    public List<TrialRecord> trials;

    public SessionUploadRequest(PlayerProfile p, PlayerSaveData s, List<TrialRecord> t)
    {
        profile = p;
        saveData = s;
        trials = t ?? new List<TrialRecord>();
    }
}