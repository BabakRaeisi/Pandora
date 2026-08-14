using System;
using UnityEngine;

public class ActiveLevelTimer
{
    private double activeSeconds;
    private double lastResumeTime;

    private bool running;
    private bool paused;

    public string StartedAtUtc { get; private set; }

    public void Start()
    {
        activeSeconds = 0;
        running = true;
        paused = false;

        lastResumeTime = Time.realtimeSinceStartupAsDouble;
        StartedAtUtc = DateTime.UtcNow.ToString("o");
    }

    public void SetPaused(bool isPaused)
    {
        if (!running)
            return;

        double now = Time.realtimeSinceStartupAsDouble;

        if (isPaused && !paused)
        {
            activeSeconds += now - lastResumeTime;
            paused = true;
        }
        else if (!isPaused && paused)
        {
            lastResumeTime = now;
            paused = false;
        }
    }

    public int StopAndGetMilliseconds()
    {
        if (!running)
            return 0;

        if (!paused)
        {
            activeSeconds +=
                Time.realtimeSinceStartupAsDouble - lastResumeTime;
        }

        running = false;

        return Mathf.RoundToInt((float)(activeSeconds * 1000.0));
    }
}