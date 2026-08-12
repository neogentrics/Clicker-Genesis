using System.Collections.Generic;
using UnityEngine;

namespace ClickerGenesis.Achievements
{
    /// <summary>Which part of the game an achievement is about - drives grouping in the (not yet
    /// built) achievements panel, same role ScribeDefinition.TierType plays for scribe rows.</summary>
    public enum AchievementCategory { BookProgress, ChapterProgress, ScribeEconomy, Managers, Prestige, Leveling, Minigame, Meta, Secret }

    /// <summary>Goal = binary unlock. Progress = numeric threshold, same split as the reference
    /// tools evaluated (Sam Carey's Achievement System, Studio-23's rebuild) - it's the right
    /// split, just re-implemented on data this project already tracks.</summary>
    public enum AchievementType { Goal, Progress }

    /// <summary>Which Bible version this achievement's condition is checked against. Most
    /// generated families (book-complete, chapter-count) exist once PER translation, not once
    /// total, once more than KJV exists - see Achievement-System-Design.html's "translation
    /// dimension" section (2026-08-08). Only KJV is wired into gameplay today.</summary>
    public enum BibleTranslation { KJV, Geneva, Ethiopian }

    /// <summary>Difficulty tier (2026-08-09, user's explicit ask - "bronze, silver, gold" diamond
    /// frames matching the MetallicUIKit pack already imported for the achievement cards). Purely
    /// a display/rank signal, not a gameplay mechanic - does not affect unlock logic at all, just
    /// which frame sprite AchievementScreenUI picks for a card.</summary>
    public enum AchievementTier { Bronze, Silver, Gold }

    /// <summary>A single scalar game stat an achievement can auto-check against, via
    /// AchievementSystem.EvaluateStat - avoids one hardcoded Notify-call per achievement id at
    /// GameLoopController's call sites; instead each stat update is reported once and every
    /// matching definition (regardless of how many share that stat) is checked in one pass.
    /// BookComplete is handled separately (EvaluateBookComplete) since it's keyed by
    /// bookResourceId, not a single number. None = this achievement is unlocked directly via
    /// Achievement System.Unlock(id) at a one-shot event (first tap, first prestige, etc.),
    /// not tracked against a running stat at all.</summary>
    public enum TrackedStat
    {
        None, LifetimeInk, ScribesOwned, ChaptersCompleted, BooksCompletedOT, ManagersUnlocked, SubmanagersUnlocked, PlayerLevel, FreePrestigeCount, ResetPrestigeCount,
        // Added 2026-08-10 for the v2 gameplay-achievement ladders (Phase 2) - TotalTaps/InkSpent
        // are lifetime cumulative counters (never regress); InkPerSecond/Grace report the current
        // live value each frame, but since EvaluateStat/SetProgress are already monotonic (a
        // Progress-type achievement's stored progress never decreases even if the reported value
        // does), reporting a live value that can naturally dip (e.g. Grace after spending it) still
        // correctly latches "highest value ever reached" - no separate lifetime-peak field needed.
        TotalTaps, InkSpent, InkPerSecond, Grace
    }

    /// <summary>One achievement. Data-only, same shape/role as ScribeDefinition - held in a list
    /// inside an AchievementSetConfig ScriptableObject, not a ScriptableObject itself. Runtime
    /// unlock/progress state lives in AchievementSystem, matching the InkWallet/ScribeSystem split
    /// of "config is data, system is state."</summary>
    [System.Serializable]
    public class AchievementDefinition
    {
        public string id;
        public string displayName;
        [TextArea] public string description;

        public AchievementCategory category;

        [Tooltip("Bronze/Silver/Gold - a difficulty rank shown as a diamond frame on the achievement card. Purely cosmetic, does not affect unlock conditions.")]
        public AchievementTier tier = AchievementTier.Bronze;

        [Tooltip("Which Bible version this achievement's condition applies to. Only KJV is wired into gameplay today - Geneva/Ethiopian-scoped achievements get authored once those translations are actually in the game, not before.")]
        public BibleTranslation translation = BibleTranslation.KJV;

        public AchievementType type = AchievementType.Goal;

        [Tooltip("Progress-type only - the numeric target. Goal-type achievements use this as the threshold value passed to EvaluateStat (e.g. \"reach level 5\"), or are unlocked directly by id for pure one-shot events (first tap, first prestige).")]
        public float progressGoal = 1f;

        [Tooltip("Progress-type only - how often an in-progress toast fires, e.g. every 25% of progressGoal. 0 = no interim notifications, only the unlock toast.")]
        public float notificationFrequency = 0f;

        [Tooltip("Which running game stat this achievement auto-checks against via AchievementSystem.EvaluateStat. None means it's unlocked directly by id at a one-shot event instead (see AchievementSystem.Unlock).")]
        public TrackedStat trackedStat = TrackedStat.None;

        [Tooltip("Only meaningful when trackedStat == None and this is a generated per-book achievement - the book this achievement's completion is checked against (matches CanonicalBookOrder resource ids, e.g. \"genesis_1\"). Checked via AchievementSystem.EvaluateBookComplete.")]
        public string bookResourceId;

        [Tooltip("Section-grouping / \"ultimate\" achievements only (e.g. \"complete the Pentateuch\") - every bookResourceId that must be complete for this achievement to unlock. Checked via AchievementSystem.EvaluateBookComplete whenever any one of them completes. Empty for every other achievement type.")]
        public List<string> requiredBookResourceIds = new List<string>();

        [Tooltip("Per-manager achievements only - matches a ScribeDefinition.managerId within the book named by bookResourceId above (e.g. \"noah\", \"paul\"). Goal-type = unlocking that specific manager. Progress-type = hiring every submanager under that manager (progressGoal = that manager's real submanager count). Checked via AchievementSystem.EvaluateManagerUnlocked / EvaluateManagerHousehold.")]
        public string managerId;

        public Sprite lockedIcon;
        public Sprite unlockedIcon;

        [Tooltip("Hidden as \"???\" until unlocked - same progressive-disclosure rule already used for the Skill Tree's node visibility and the pre-prestige Books tab. Used for later-book/later-character achievements that would otherwise spoil what's coming.")]
        public bool spoiler;

        [Tooltip("Flags a \"deep trivia\" achievement whose wording depends on a specific textual detail that needs checking against a real study Bible/reference before it's authored - same \"don't invent scripture-accuracy claims unilaterally\" rule as the Character Index work. Never auto-generated.")]
        public bool requiresManualVerification;

        [Tooltip("Marks this entry as procedurally generated (and which generator produced it, e.g. \"book-complete\") vs. hand-authored, so tooling can regenerate the generated set without touching hand-authored entries. Empty = hand-authored.")]
        public string generatedFrom;
    }
}
