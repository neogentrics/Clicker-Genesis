using System;

namespace ClickerGenesis.Economy
{
    /// <summary>
    /// Tracks owned counts per scribe tier and derives cost/production from ScribeSetConfig.
    ///
    /// Design simplification vs. the original notes (flagged here deliberately, not silently):
    /// scribes are purely passive Ink/sec generators once owned (no manual re-click needed —
    /// this matches the dominant "passive Ink/sec generators" framing). A manager, once its
    /// player-level threshold is reached, doesn't "auto-click" anything (there's nothing to
    /// click) — instead it grants a flat output bonus to its own tier. This keeps managers
    /// meaningfully tied to player level (as originally specified) without a mechanic
    /// ("auto-triggers so you don't have to manually click") that doesn't apply to a
    /// continuously-passive system.
    /// </summary>
    public class ScribeSystem
    {
        private readonly ScribeSetConfig config;
        private readonly int[] owned;
        private readonly bool[] managerUnlocked;

        public ScribeSystem(ScribeSetConfig config)
        {
            this.config = config;
            owned = new int[config.tiers.Count];
            managerUnlocked = new bool[config.tiers.Count];
        }

        public int TierCount => config.tiers.Count;

        public ScribeDefinition GetDefinition(int tierIndex) => config.tiers[tierIndex];

        public int GetOwned(int tierIndex) => owned[tierIndex];

        /// <summary>Whether the player has bought enough verses to unlock this tier for purchase.</summary>
        public bool IsUnlocked(int tierIndex, int currentVerseIndex) =>
            currentVerseIndex >= config.tiers[tierIndex].unlockAtVerseIndex;

        public float GetOwnedMilestoneMultiplier(int tierIndex) => MilestoneCurve.GetMultiplier(owned[tierIndex]);

        public double GetNextCost(int tierIndex)
        {
            var def = config.tiers[tierIndex];
            return def.baseCost * Math.Pow(def.costGrowthRate, owned[tierIndex]);
        }

        /// <summary>Whether this tier's manager is bought AND actively boosting output (also
        /// needs at least one scribe of the tier owned - a manager with nothing to manage
        /// contributes nothing).</summary>
        public bool IsManagerActive(int tierIndex, int playerLevel)
        {
            var def = config.tiers[tierIndex];
            return def.HasManager && managerUnlocked[tierIndex] && owned[tierIndex] > 0;
        }

        public bool IsManagerUnlocked(int tierIndex) => managerUnlocked[tierIndex];

        /// <summary>How many managers have been bought across every tier - for the Stats screen
        /// (2026-08-06).</summary>
        public int UnlockedManagerCount()
        {
            int count = 0;
            for (int i = 0; i < managerUnlocked.Length; i++)
                if (managerUnlocked[i]) count++;
            return count;
        }

        /// <summary>Whether the level threshold has been reached, independent of whether the
        /// manager has actually been bought yet - REVISED 2026-08-04, managers require both the
        /// level AND an Ink purchase, not level alone.</summary>
        public bool IsManagerLevelEligible(int tierIndex, int playerLevel)
        {
            var def = config.tiers[tierIndex];
            return def.HasManager && playerLevel >= def.managerUnlockLevel;
        }

        /// <summary>A manager can only be bought once its OWN scribe tier is unlocked (verse
        /// progress), not just the level threshold - fixes a real bug where a manager (e.g. Noah)
        /// could be bought and would immediately show as "takes over at level N" on a scribe tier
        /// (e.g. Ark's Manifest) the player hadn't unlocked yet.</summary>
        public bool CanUnlockManager(int tierIndex, int playerLevel, int currentVerseIndex) =>
            IsManagerLevelEligible(tierIndex, playerLevel) && !managerUnlocked[tierIndex] && IsUnlocked(tierIndex, currentVerseIndex);

        /// <summary>Marks a manager as unlocked. Caller is responsible for spending Ink first
        /// (same pattern as Buy for scribe tiers).</summary>
        public void UnlockManager(int tierIndex)
        {
            managerUnlocked[tierIndex] = true;
        }

        /// <summary>Increments the owned count for a tier. Caller is responsible for spending Ink first.</summary>
        public void Buy(int tierIndex)
        {
            owned[tierIndex]++;
        }

        /// <summary>Wipes every tier's owned count back to zero - the opt-in prestige reset path's
        /// "upgrade levels" reset. Managers stay unlocked (not reset) - re-buying every manager
        /// after every reset was never specified and would feel punishing; treat manager unlocks
        /// as permanent, same as unlocked verses/chapters/books.</summary>
        public void ResetOwned()
        {
            for (int i = 0; i < owned.Length; i++) owned[i] = 0;
        }

        /// <summary>progressMultiplier is GameLoopController.ProgressMultiplier - a shared passive
        /// bonus from verse/chapter progress that applies on top of this tier's own owned-count
        /// milestone curve and manager bonus (2026-08-04, explicit user design).</summary>
        /// <summary>managerBonusBoost is an additive percentage from the Grace skill tree's
        /// "Overseer's Wisdom" branch (GameLoopController.EffectiveInkPerSecond) - stacks on top of
        /// this tier's own fixed manager bonus rather than replacing it.</summary>
        public double GetTierInkPerSecond(int tierIndex, int playerLevel, float progressMultiplier = 1f, double managerBonusBoost = 0)
        {
            var def = config.tiers[tierIndex];
            double output = def.baseInkPerSecond * owned[tierIndex] * GetOwnedMilestoneMultiplier(tierIndex) * progressMultiplier;
            if (IsManagerActive(tierIndex, playerLevel))
                output *= 1.0 + def.managerBonusMultiplier + managerBonusBoost;
            return output;
        }

        public double TotalInkPerSecond(int playerLevel, float progressMultiplier = 1f, double managerBonusBoost = 0)
        {
            double total = 0;
            for (int i = 0; i < TierCount; i++)
                total += GetTierInkPerSecond(i, playerLevel, progressMultiplier, managerBonusBoost);
            return total;
        }
    }
}
