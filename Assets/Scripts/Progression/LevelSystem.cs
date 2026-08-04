using System;

namespace ClickerGenesis.Progression
{
    /// <summary>
    /// Tracks accumulated XP and derives player level from it.
    /// XP required to go from level L to L+1 = config.LevelXpBase * L (linear growth),
    /// so cumulative XP at the start of level L = LevelXpBase * (L-1) * L / 2.
    /// </summary>
    public class LevelSystem
    {
        private readonly XpConfig config;

        public int TotalXp { get; private set; }
        public int CurrentLevel { get; private set; } = 1;

        public event Action<int> OnLevelUp;

        public LevelSystem(XpConfig config)
        {
            this.config = config;
        }

        public bool IsPrestigeEligible => CurrentLevel >= config.PrestigeLevelThreshold;

        /// <summary>
        /// True once the player is close enough that showing the (still locked) Prestige button
        /// is a meaningful goal rather than clutter — one level below the threshold. Progressive
        /// disclosure: far-off features stay hidden entirely rather than visible-but-locked from
        /// the very start.
        /// </summary>
        public bool IsPrestigeNear => CurrentLevel >= config.PrestigeLevelThreshold - 1;

        public int XpAtLevelStart(int level) => config.LevelXpBase * (level - 1) * level / 2;

        public int XpRequiredForNextLevel => config.LevelXpBase * CurrentLevel;

        public int XpIntoCurrentLevel => TotalXp - XpAtLevelStart(CurrentLevel);

        public void AddXp(int amount)
        {
            if (amount <= 0) return;

            TotalXp += amount;
            while (TotalXp >= XpAtLevelStart(CurrentLevel + 1))
            {
                CurrentLevel++;
                OnLevelUp?.Invoke(CurrentLevel);
            }
        }
    }
}
