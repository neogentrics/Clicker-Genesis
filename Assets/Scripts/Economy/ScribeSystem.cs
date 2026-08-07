using System;
using System.Collections.Generic;

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
        /// <summary>Owned submanagers per tier, indexed [tierIndex][submanagerIndex] - most tiers
        /// have zero submanagers so this stays a small sparse array of bool arrays (2026-08-06).</summary>
        private readonly bool[][] submanagerOwned;

        public ScribeSystem(ScribeSetConfig config)
        {
            this.config = config;
            owned = new int[config.tiers.Count];
            managerUnlocked = new bool[config.tiers.Count];
            submanagerOwned = new bool[config.tiers.Count][];
            for (int i = 0; i < config.tiers.Count; i++)
                submanagerOwned[i] = new bool[config.tiers[i].submanagers?.Count ?? 0];
        }

        public int TierCount => config.tiers.Count;

        public ScribeDefinition GetDefinition(int tierIndex) => config.tiers[tierIndex];

        /// <summary>Resolves a manager id (e.g. "paul") to that manager's display name (e.g.
        /// "Paul") within THIS book's roster only - used by a Law tier's deliveredByManagerId
        /// attribution (2026-08-08, per CharacterIndex-Integration-Mapping.md §8). Returns null if
        /// no tier in this book's config has a matching managerId - a law tier's deliveredBy is
        /// attribution only, not a hard reference, so a miss should degrade gracefully (omit the
        /// "as taught by" line) rather than throw.</summary>
        public string GetManagerDisplayName(string managerId)
        {
            if (string.IsNullOrEmpty(managerId)) return null;
            foreach (var tier in config.tiers)
                if (tier.managerId == managerId)
                    return tier.managerName;
            return null;
        }

        public int GetOwned(int tierIndex) => owned[tierIndex];

        /// <summary>Whether the player has bought enough verses to unlock this tier for purchase.</summary>
        public bool IsUnlocked(int tierIndex, int currentVerseIndex) =>
            currentVerseIndex >= config.tiers[tierIndex].unlockAtVerseIndex;

        public float GetOwnedMilestoneMultiplier(int tierIndex) => MilestoneCurve.GetMultiplier(owned[tierIndex]);

        public double GetNextCost(int tierIndex)
        {
            var def = config.tiers[tierIndex];
            double raw = def.baseCost * Math.Pow(def.costGrowthRate, owned[tierIndex]);
            return raw * (1.0 - GetCostCutterDiscount(tierIndex));
        }

        /// <summary>Sum of every owned "cost-cutter" submanager's perkAmount on this tier, clamped
        /// so stacking several can never make a purchase free or negative (2026-08-06).</summary>
        private double GetCostCutterDiscount(int tierIndex)
        {
            var def = config.tiers[tierIndex];
            if (def.submanagers == null) return 0;
            double discount = 0;
            for (int i = 0; i < def.submanagers.Count; i++)
                if (submanagerOwned[tierIndex][i] && def.submanagers[i].perkFlavor == "cost-cutter")
                    discount += def.submanagers[i].perkAmount;
            return Math.Min(discount, 0.75);
        }

        /// <summary>Sum of every owned "loyalty-boost" submanager's perkAmount on this tier - added
        /// on top of the tier's own managerBonusMultiplier in GetTierInkPerSecond (2026-08-06).</summary>
        private double GetLoyaltyBoost(int tierIndex)
        {
            var def = config.tiers[tierIndex];
            if (def.submanagers == null) return 0;
            double boost = 0;
            for (int i = 0; i < def.submanagers.Count; i++)
                if (submanagerOwned[tierIndex][i] && def.submanagers[i].perkFlavor == "loyalty-boost")
                    boost += def.submanagers[i].perkAmount;
            return boost;
        }

        public int SubmanagerCount(int tierIndex) => config.tiers[tierIndex].submanagers?.Count ?? 0;

        public SubmanagerDefinition GetSubmanagerDefinition(int tierIndex, int subIndex) =>
            config.tiers[tierIndex].submanagers[subIndex];

        public bool IsSubmanagerOwned(int tierIndex, int subIndex) => submanagerOwned[tierIndex][subIndex];

        /// <summary>A submanager can only be hired once their own character's verse is reached AND
        /// this tier's own manager is already active - a submanager assisting a manager who isn't
        /// there yet doesn't make sense (2026-08-06).</summary>
        public bool CanBuySubmanager(int tierIndex, int subIndex, int currentVerseIndex, int playerLevel)
        {
            if (submanagerOwned[tierIndex][subIndex]) return false;
            if (!managerUnlocked[tierIndex]) return false;
            var sub = config.tiers[tierIndex].submanagers[subIndex];
            return currentVerseIndex >= sub.unlockAtVerseIndex;
        }

        /// <summary>Marks a submanager as hired. Caller is responsible for spending Ink first (same
        /// pattern as Buy/UnlockManager).</summary>
        public void BuySubmanager(int tierIndex, int subIndex)
        {
            submanagerOwned[tierIndex][subIndex] = true;
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
        /// (e.g. Ark's Manifest) the player hadn't unlocked yet. ALSO requires the manager's own
        /// character has actually been reached in scripture (managerUnlockAtVerseIndex, real
        /// CharacterIndex data, 2026-08-06) - deliberately separate from the scribe tier's own
        /// unlockAtVerseIndex, which stays a much lower smooth-pacing value so the scribe tier
        /// itself is still buyable long before its manager's real narrative moment arrives (a real
        /// bug: using the same field for both meant Reed Pen, the starter scribe, couldn't be
        /// bought until verse 26, since that's Adam's own first mention).</summary>
        public bool IsManagerVerseReached(int tierIndex, int currentVerseIndex) =>
            currentVerseIndex >= config.tiers[tierIndex].managerUnlockAtVerseIndex;

        public bool CanUnlockManager(int tierIndex, int playerLevel, int currentVerseIndex) =>
            IsManagerLevelEligible(tierIndex, playerLevel) && !managerUnlocked[tierIndex]
            && IsUnlocked(tierIndex, currentVerseIndex) && IsManagerVerseReached(tierIndex, currentVerseIndex);

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
                output *= 1.0 + def.managerBonusMultiplier + managerBonusBoost + GetLoyaltyBoost(tierIndex);
            return output;
        }

        public double TotalInkPerSecond(int playerLevel, float progressMultiplier = 1f, double managerBonusBoost = 0)
        {
            double total = 0;
            for (int i = 0; i < TierCount; i++)
                total += GetTierInkPerSecond(i, playerLevel, progressMultiplier, managerBonusBoost);
            return total;
        }

        /// <summary>Snapshots owned/managerUnlocked/submanagerOwned for a save file (2026-08-08).
        /// Cloned, not the live arrays - the caller may hold this across a frame boundary while
        /// serializing.</summary>
        public int[] ExportOwned() => (int[])owned.Clone();
        public bool[] ExportManagerUnlocked() => (bool[])managerUnlocked.Clone();
        public bool[][] ExportSubmanagerOwned()
        {
            var result = new bool[submanagerOwned.Length][];
            for (int i = 0; i < submanagerOwned.Length; i++)
                result[i] = (bool[])submanagerOwned[i].Clone();
            return result;
        }

        /// <summary>Restores owned/managerUnlocked/submanagerOwned from a save file (2026-08-08).
        /// Length-bounds every copy against THIS book's current config rather than trusting the
        /// saved array lengths - a roster can grow (more tiers/submanagers added to a book after
        /// the save was written) or, in principle, shrink, and an old save should load whatever
        /// still applies rather than throwing.</summary>
        public void ImportState(int[] savedOwned, bool[] savedManagerUnlocked, bool[][] savedSubmanagerOwned)
        {
            if (savedOwned != null)
                for (int i = 0; i < owned.Length && i < savedOwned.Length; i++)
                    owned[i] = savedOwned[i];

            if (savedManagerUnlocked != null)
                for (int i = 0; i < managerUnlocked.Length && i < savedManagerUnlocked.Length; i++)
                    managerUnlocked[i] = savedManagerUnlocked[i];

            if (savedSubmanagerOwned != null)
                for (int i = 0; i < submanagerOwned.Length && i < savedSubmanagerOwned.Length; i++)
                {
                    var savedTierSubs = savedSubmanagerOwned[i];
                    if (savedTierSubs == null) continue;
                    for (int j = 0; j < submanagerOwned[i].Length && j < savedTierSubs.Length; j++)
                        submanagerOwned[i][j] = savedTierSubs[j];
                }
        }
    }
}
