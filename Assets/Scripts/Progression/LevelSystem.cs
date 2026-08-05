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

        public int PrestigeLevelThreshold => config.PrestigeLevelThreshold;

        /// <summary>
        /// True once the player has reached XpConfig.PrestigeButtonVisibleLevel (2026-08-04:
        /// user's explicit call — level 2, well before the level-5 unlock threshold) — the
        /// (still locked) Prestige button shows up as a goal to work toward, rather than only
        /// appearing right at (or one level before) the threshold itself. Progressive disclosure:
        /// far-off features stay hidden entirely rather than visible-but-locked from level 1.
        /// </summary>
        public bool IsPrestigeNear => CurrentLevel >= config.PrestigeButtonVisibleLevel;

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

        /// <summary>Resets level/XP back to the start - only called on the opt-in RESET prestige
        /// path (2026-08-05, explicit user correction: the free path must never touch the XP bar,
        /// reversing the original "resets on every cycle" design). Bundled with the reset path's
        /// Ink/scribe-owned-count wipe as one "start over for bonus Grace" action.</summary>
        public void ResetForPrestige()
        {
            TotalXp = 0;
            CurrentLevel = 1;
        }
    }
}
