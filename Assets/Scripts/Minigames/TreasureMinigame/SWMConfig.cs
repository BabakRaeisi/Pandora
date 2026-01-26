using UnityEngine;

[CreateAssetMenu(menuName = "SWM/SWM Config", fileName = "SWMConfig")]
public class SWMConfig : ScriptableObject
{
    [System.Serializable]
    public struct DayConfig
    {
        public int day;              // 1..7
        public int boxes;
        public int treasures;
        public int trials;           // per session day
    }

    public DayConfig[] days = new DayConfig[]
    {
        new DayConfig{ day=1, boxes=3, treasures=2, trials=5 },
        new DayConfig{ day=2, boxes=4, treasures=2, trials=5 },
        new DayConfig{ day=3, boxes=4, treasures=3, trials=6 },
        new DayConfig{ day=4, boxes=6, treasures=3, trials=6 },
        new DayConfig{ day=5, boxes=6, treasures=4, trials=7 },
        new DayConfig{ day=6, boxes=6, treasures=4, trials=7 },
        new DayConfig{ day=7, boxes=8, treasures=4, trials=8 },
    };

    public DayConfig GetDay(int day)
    {
        day = Mathf.Clamp(day, 1, 7);
        for (int i = 0; i < days.Length; i++)
            if (days[i].day == day) return days[i];

        return days[day - 1];
    }
}
