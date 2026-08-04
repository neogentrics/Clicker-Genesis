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

        public ScribeSystem(ScribeSetConfig config)
        {
            this.config = config;
            owned = new int[config.tiers.Count];
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

        public bool IsManagerActive(int tierIndex, int playerLevel)
        {
            var def = config.tiers[tierIndex];
            return def.HasManager && owned[tierIndex] > 0 && playerLevel >= def.managerUnlockLevel;
        }

        /// <summary>Increments the owned count for a tier. Caller is responsible for spending Ink first.</summary>
        public void Buy(int tierIndex)
        {
            owned[tierIndex]++;
        }

        public double GetTierInkPerSecond(int tierIndex, int playerLevel)
        {
            var def = config.tiers[tierIndex];
            double output = def.baseInkPerSecond * owned[tierIndex] * GetOwnedMilestoneMultiplier(tierIndex);
            if (IsManagerActive(tierIndex, playerLevel))
                output *= 1.0 + def.managerBonusMultiplier;
            return output;
        }

        public double TotalInkPerSecond(int playerLevel)
        {
            double total = 0;
            for (int i = 0; i < TierCount; i++)
                total += GetTierInkPerSecond(i, playerLevel);
            return total;
        }
    }
}
