using System;
using System.Collections.Generic;

namespace ClickerGenesis.Save
{
    /// <summary>
    /// Root shape for the GLOBAL achievement ledger (2026-08-08) - deliberately a separate file
    /// from SaveData/the per-slot save system. Achievement unlocks must persist across every save
    /// slot (completing a translation on Slot 1 must be visible from a fresh Slot 2), so this data
    /// cannot live inside SaveData's per-slot shape. Same sectioning/versioning discipline as
    /// SaveData - only unlocked ids and non-zero progress values are stored, everything else is
    /// the default state a fresh AchievementSystem already starts at.
    /// </summary>
    [Serializable]
    public class AchievementData
    {
        public int dataVersion;
        public string savedAtUtc;

        public List<string> unlockedIds = new List<string>();
        public List<AchievementProgressEntry> progress = new List<AchievementProgressEntry>();
    }

    [Serializable]
    public class AchievementProgressEntry
    {
        public string id;
        public float value;
    }
}
