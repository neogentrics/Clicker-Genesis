using System;
using System.Collections.Generic;

namespace ClickerGenesis.Save
{
    /// <summary>
    /// Root save-file shape (2026-08-08, per Save-System-Design.md). Sectioned so a future field
    /// addition only touches the section it belongs to, and so SaveMigrator only has to reason
    /// about the section that actually changed shape. Settings (sound, resolution, quality, etc.)
    /// are deliberately NOT included here — GameSettings already persists them independently via
    /// PlayerPrefs, which survives exactly the same "close and relaunch" scenario this file exists
    /// for; duplicating them here would just create a second source of truth with no benefit.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public int saveVersion;
        public string savedAtUtc;

        public EconomyState economy = new EconomyState();
        public ProgressionState progression = new ProgressionState();
        public PrestigeState prestige = new PrestigeState();
        public List<BookProgressEntry> books = new List<BookProgressEntry>();
        public string activeBookResourceId;

        /// <summary>The book the player originally started this slot in (2026-08-08, Skill Tree
        /// redesign) - set once at New Game and never changed afterward, even if activeBookResourceId
        /// later moves via SwitchActiveBook. The Grace tree's Book Progression branch is anchored to
        /// this, not to whichever book happens to be active, so re-loading a save always rebuilds
        /// the exact same cost/prerequisite shape the player has already spent Grace against.</summary>
        public string startingBookResourceId;
    }

    [Serializable]
    public class EconomyState
    {
        public double inkBalance;
        public double inkLifetimeEarned;
        public double inkTotalSpent;
        public int clickPowerLevel;
        public float progressMultiplier = 1f;
        public List<BookScribeState> scribeBooks = new List<BookScribeState>();
    }

    /// <summary>One book's scribe roster state - owned counts, manager unlocks, submanager
    /// ownership. Keyed by bookResourceId (e.g. "genesis_1"), matching GameLoopController's
    /// scribeSystemsByBook/bookProgress dictionary keys.</summary>
    [Serializable]
    public class BookScribeState
    {
        public string bookResourceId;
        public List<int> owned = new List<int>();
        public List<bool> managerUnlocked = new List<bool>();
        /// <summary>One entry per tier, each holding that tier's submanager ownership flags.
        /// JsonUtility can't serialize a nested List&lt;List&lt;bool&gt;&gt; directly, hence the
        /// wrapper class per tier.</summary>
        public List<TierSubmanagerState> submanagerOwned = new List<TierSubmanagerState>();
    }

    [Serializable]
    public class TierSubmanagerState
    {
        public List<bool> owned = new List<bool>();
    }

    [Serializable]
    public class ProgressionState
    {
        public int totalXp;
        public int currentLevel = 1;
    }

    [Serializable]
    public class PrestigeState
    {
        public double grace;
        public double graceEverSpent;
        public int freePrestigeCount;
        public int resetPrestigeCount;
        /// <summary>Every purchased Grace skill node id + its rank. List-of-pairs, not a
        /// Dictionary - JsonUtility doesn't support Dictionary&lt;,&gt; directly.</summary>
        public List<SkillRankEntry> skillRanks = new List<SkillRankEntry>();

        /// <summary>Which book each generic "Unlock New Book" node's rank was spent on
        /// (2026-08-09, player-choice book unlocking redesign) - list-of-pairs, same reason as
        /// skillRanks above.</summary>
        public List<BookChoiceEntry> bookChoices = new List<BookChoiceEntry>();
    }

    [Serializable]
    public class SkillRankEntry
    {
        public string nodeId;
        public int rank;
    }

    [Serializable]
    public class BookChoiceEntry
    {
        public string nodeId;
        public string bookResourceId;
    }

    /// <summary>One entry per book the player has EVER touched, not just the active one -
    /// switching books never resets another book's progress, so a save that only captured the
    /// active book would silently lose every other book's progress the next time the player
    /// switched back to it.</summary>
    [Serializable]
    public class BookProgressEntry
    {
        public string resourceId;
        public int nextVerseIndex;
        public int unlockedChapterNumber = -1;
        public int chaptersCompletedInBook;
    }
}
