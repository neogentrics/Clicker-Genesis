using System;
using System.Collections.Generic;

namespace ClickerGenesis.Save
{
    /// <summary>
    /// Root shape for the achievement ledger - one per save slot as of 2026-08-10 (was a single
    /// GLOBAL file shared across every slot; reversed per real user correction, see
    /// AchievementFileStorage's doc comment for why). Still deliberately a separate file from
    /// SaveData - keeps the achievement ledger's own versioning/migration chain independent, just
    /// no longer a single shared file. Same sectioning/versioning discipline as SaveData - only
    /// unlocked ids and non-zero progress values are stored, everything else is the default state
    /// a fresh AchievementSystem already starts at.
    /// </summary>
    [Serializable]
    public class AchievementData
    {
        public int dataVersion;
        public string savedAtUtc;

        /// <summary>v1 shape (pre-2026-08-10) - kept ONLY so AchievementDataMigrator can read an
        /// old file written before unlock timestamps existed. New saves never populate this;
        /// always write to `unlocks` instead.</summary>
        public List<string> unlockedIds = new List<string>();

        /// <summary>v2 shape (2026-08-10) - id + real unlock timestamp, replacing the plain
        /// `unlockedIds` string list above.</summary>
        public List<AchievementUnlockEntry> unlocks = new List<AchievementUnlockEntry>();

        public List<AchievementProgressEntry> progress = new List<AchievementProgressEntry>();
    }

    [Serializable]
    public class AchievementUnlockEntry
    {
        public string id;

        /// <summary>ISO 8601 UTC timestamp ("o" format), same convention as SaveData/AchievementData's
        /// own savedAtUtc. Empty string for achievements migrated from a pre-timestamp save - their
        /// real unlock moment was never recorded, so this is left blank rather than guessed.</summary>
        public string unlockedAtUtc;
    }

    [Serializable]
    public class AchievementProgressEntry
    {
        public string id;
        public float value;
    }
}
