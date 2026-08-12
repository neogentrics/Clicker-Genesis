using System;
using System.Collections.Generic;
using System.Linq;

namespace ClickerGenesis.Achievements
{
    /// <summary>
    /// Tracks unlock/progress state for every achievement across every AchievementSetConfig it was
    /// built from. Deliberately event-driven, not a per-frame poll of every definition - this
    /// project has been burned twice by per-frame rebuild lag (bug #22; the PermanentUpgradesListUI
    /// fix). Call sites report a stat change once (EvaluateStat) or a one-shot event (Unlock)
    /// exactly where that event already happens in GameLoopController - matching the XP-award
    /// call-site pattern already used throughout that class.
    ///
    /// PER-SAVE-SLOT (REVISED 2026-08-10, was GLOBAL): originally built as a single ledger shared
    /// across every save slot so progress carried between slots. Real user reversal - a global
    /// ledger meant starting a new game, or deleting the current one, could never actually "start
    /// over," since already-earned achievements would still show. GameLoopController now
    /// reconstructs this instance on every slot switch (SwitchToSlot), same as every other
    /// per-slot system - it is NOT exempt from that the way it used to be.
    /// </summary>
    public class AchievementSystem
    {
        private readonly Dictionary<string, AchievementDefinition> definitions = new Dictionary<string, AchievementDefinition>();
        private readonly Dictionary<string, bool> unlocked = new Dictionary<string, bool>();
        private readonly Dictionary<string, float> progress = new Dictionary<string, float>();

        /// <summary>Real UTC unlock timestamp per achievement id ("o"-format ISO 8601 string, same
        /// convention as SaveData/AchievementData's own savedAtUtc), added 2026-08-10 for the
        /// planned trophy-case gallery's "when earned" display. Only populated for ids actually
        /// present in `unlocked` - never invented for a migrated pre-timestamp entry (see
        /// AchievementDataMigrator).</summary>
        private readonly Dictionary<string, string> unlockedAtUtc = new Dictionary<string, string>();

        /// <summary>Every bookResourceId ever reported complete via EvaluateBookComplete - local
        /// state (not persisted directly; re-derived by GameLoopController replaying
        /// EvaluateBookComplete for every already-complete book right after a load, see Awake()).
        /// Backs the section-grouping / "ultimate" achievements' AND-across-several-books check.</summary>
        private readonly HashSet<string> completedBooks = new HashSet<string>();

        /// <summary>Fired once, the moment an achievement unlocks - the toast/notification UI's
        /// hook once that UI exists. Not fired again on repeated Unlock() calls for an
        /// already-unlocked id.</summary>
        public event Action<AchievementDefinition> OnAchievementUnlocked;

        /// <summary>Fired when a Progress-type achievement's value increases (but hasn't reached
        /// its goal yet) - the interim "in-progress" toast hook, gated by notificationFrequency in
        /// the UI layer, not here.</summary>
        public event Action<AchievementDefinition, float> OnAchievementProgress;

        public AchievementSystem(IEnumerable<AchievementSetConfig> configs)
        {
            foreach (var cfg in configs)
            {
                if (cfg == null) continue;
                foreach (var def in cfg.achievements)
                {
                    if (string.IsNullOrEmpty(def.id) || definitions.ContainsKey(def.id)) continue;
                    definitions[def.id] = def;
                    unlocked[def.id] = false;
                    progress[def.id] = 0f;
                }
            }
        }

        public IReadOnlyCollection<AchievementDefinition> AllDefinitions => definitions.Values;

        public bool Exists(string id) => definitions.ContainsKey(id);

        public bool IsUnlocked(string id) => unlocked.TryGetValue(id, out var u) && u;

        public float GetProgress(string id) => progress.TryGetValue(id, out var p) ? p : 0f;

        public int TotalCount => definitions.Count;

        public int GetAchievedCount() => unlocked.Values.Count(v => v);

        public float GetAchievedPercentage() => TotalCount == 0 ? 0f : (float)GetAchievedCount() / TotalCount * 100f;

        /// <summary>Fully unlocks an achievement directly by id - the one-shot-event path (first
        /// tap, first scribe, first manager, first prestige, etc.), called straight from
        /// GameLoopController at the exact place that event already happens. No-ops on an unknown
        /// or already-unlocked id.</summary>
        public void Unlock(string id)
        {
            if (!definitions.TryGetValue(id, out var def) || IsUnlocked(id)) return;
            unlocked[id] = true;
            progress[id] = def.type == AchievementType.Progress ? Math.Max(def.progressGoal, 1f) : 1f;
            unlockedAtUtc[id] = DateTime.UtcNow.ToString("o");
            OnAchievementUnlocked?.Invoke(def);
        }

        /// <summary>Real unlock moment for an already-unlocked achievement, or null if unknown
        /// (never unlocked, or migrated from a save written before timestamps existed).</summary>
        public string GetUnlockedAtUtc(string id) => unlockedAtUtc.TryGetValue(id, out var t) ? t : null;

        /// <summary>Sets a Progress-type achievement's value directly, clamped to its goal and
        /// never allowed to regress (a save-load or a stat recompute should never un-progress an
        /// achievement). Auto-unlocks once the value reaches progressGoal. No-ops for Goal-type
        /// achievements or unknown ids - use Unlock() for those.</summary>
        public void SetProgress(string id, float value)
        {
            if (!definitions.TryGetValue(id, out var def) || def.type != AchievementType.Progress || IsUnlocked(id)) return;
            float clamped = Math.Min(value, def.progressGoal);
            if (clamped <= progress[id]) return;
            progress[id] = clamped;
            OnAchievementProgress?.Invoke(def, clamped);
            if (clamped >= def.progressGoal) Unlock(id);
        }

        /// <summary>Checks every definition tracking the given stat against value in one pass -
        /// the generic alternative to hardcoding one Notify-call per achievement id.
        /// Goal-type definitions on this stat unlock once value reaches their progressGoal;
        /// Progress-type definitions call SetProgress. Cheap (a handful of float comparisons over
        /// however many definitions share this stat, typically single digits) - safe to call from
        /// a per-frame path like passive Ink income, unlike a UI rebuild.</summary>
        public void EvaluateStat(TrackedStat stat, float value)
        {
            if (stat == TrackedStat.None) return;
            foreach (var def in definitions.Values)
            {
                if (def.trackedStat != stat || IsUnlocked(def.id)) continue;
                if (def.type == AchievementType.Progress) SetProgress(def.id, value);
                else if (value >= def.progressGoal) Unlock(def.id);
            }
        }

        /// <summary>Checks every generated per-book achievement whose bookResourceId matches
        /// against being newly complete - called wherever GameLoopController detects
        /// BookComplete having gone true for the active book. Also records the book into
        /// completedBooks and checks every section-grouping/"ultimate" achievement whose
        /// requiredBookResourceIds are now all satisfied. Cheap same as EvaluateStat - safe to
        /// call repeatedly for the same already-complete book (Unlock() no-ops on an id that's
        /// already unlocked, and completedBooks is a HashSet).</summary>
        public void EvaluateBookComplete(string bookResourceId)
        {
            if (string.IsNullOrEmpty(bookResourceId)) return;
            completedBooks.Add(bookResourceId);

            foreach (var def in definitions.Values)
            {
                if (IsUnlocked(def.id)) continue;

                if (def.bookResourceId == bookResourceId) Unlock(def.id);

                if (def.requiredBookResourceIds != null && def.requiredBookResourceIds.Count > 0
                    && completedBooks.IsSupersetOf(def.requiredBookResourceIds))
                    Unlock(def.id);
            }
        }

        /// <summary>Unlocks the specific per-manager achievement for (bookResourceId, managerId) -
        /// the Goal-type family ("A Calling Answered: Noah"). No-ops if no matching definition or
        /// already unlocked.</summary>
        public void EvaluateManagerUnlocked(string bookResourceId, string managerId)
        {
            if (string.IsNullOrEmpty(bookResourceId) || string.IsNullOrEmpty(managerId)) return;
            foreach (var def in definitions.Values)
            {
                if (IsUnlocked(def.id) || def.type != AchievementType.Goal) continue;
                if (def.bookResourceId == bookResourceId && def.managerId == managerId) Unlock(def.id);
            }
        }

        /// <summary>Updates progress toward the "household complete" achievement for
        /// (bookResourceId, managerId) - the Progress-type family ("Noah's household: 3/3
        /// submanagers hired"). ownedSubCount is the caller's current count for that manager's
        /// submanagers; SetProgress auto-unlocks once it reaches the definition's own
        /// progressGoal (that manager's real submanager count, set at authoring time).</summary>
        public void EvaluateManagerHousehold(string bookResourceId, string managerId, int ownedSubCount)
        {
            if (string.IsNullOrEmpty(bookResourceId) || string.IsNullOrEmpty(managerId)) return;
            foreach (var def in definitions.Values)
            {
                if (IsUnlocked(def.id) || def.type != AchievementType.Progress) continue;
                if (def.bookResourceId == bookResourceId && def.managerId == managerId) SetProgress(def.id, ownedSubCount);
            }
        }

        /// <summary>Snapshots unlocked ids + non-zero progress values for AchievementData
        /// (2026-08-08) - only unlocked==true ids and progress>0 values are worth persisting,
        /// everything else is the default state a fresh AchievementSystem already starts at.</summary>
        public IEnumerable<string> ExportUnlockedIds() => unlocked.Where(kvp => kvp.Value).Select(kvp => kvp.Key);

        public IEnumerable<KeyValuePair<string, float>> ExportProgress() => progress.Where(kvp => kvp.Value > 0f);

        /// <summary>Every unlocked id's real UTC timestamp, for persisting alongside
        /// ExportUnlockedIds() (2026-08-10).</summary>
        public IEnumerable<KeyValuePair<string, string>> ExportUnlockTimestamps() => unlockedAtUtc;

        /// <summary>Restores unlocked ids + progress values + unlock timestamps from
        /// AchievementData. Unknown ids (e.g. an achievement removed from config since the data
        /// was saved) are silently skipped rather than throwing - same "load whatever still
        /// applies" rule ScribeSystem's ImportState follows for a roster that's grown/shrunk since
        /// the save was written. savedTimestamps is optional so older call sites/tests that don't
        /// track timestamps still compile.</summary>
        public void ImportState(IEnumerable<string> savedUnlockedIds, IEnumerable<KeyValuePair<string, float>> savedProgress,
            IEnumerable<KeyValuePair<string, string>> savedTimestamps = null)
        {
            foreach (var id in savedUnlockedIds)
                if (definitions.ContainsKey(id)) unlocked[id] = true;

            foreach (var kvp in savedProgress)
                if (definitions.ContainsKey(kvp.Key)) progress[kvp.Key] = kvp.Value;

            if (savedTimestamps != null)
                foreach (var kvp in savedTimestamps)
                    if (definitions.ContainsKey(kvp.Key) && !string.IsNullOrEmpty(kvp.Value)) unlockedAtUtc[kvp.Key] = kvp.Value;
        }
    }
}
