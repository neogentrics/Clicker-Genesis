using System;

namespace ClickerGenesis.Progression
{
    /// <summary>
    /// Grace currency + prestige reward math, per the full spec on the Notion Progression subpage
    /// (confirmed 2026-08-03). In-memory only, same as every other stat in this project - a real
    /// save system is a separate, explicitly deferred item (2026-08-04, user's own call after
    /// reopening the question when Prestige came up).
    ///
    /// GameLoopController owns the actual reset flow (Wallet/Levels/Scribes) - this class only
    /// tracks Grace itself and the reward formula, so it stays a plain, testable C# class like
    /// InkWallet/LevelSystem/ScribeSystem.
    /// </summary>
    public class PrestigeSystem
    {
        public double Grace { get; private set; }

        /// <summary>Lifetime Grace spent on anything (skill tree nodes, etc.) - tracked separately
        /// from the current Grace balance for potential future use (analytics, achievements), even
        /// though nothing currently reads it: it used to drive a flat "+1% Ink per Grace spent"
        /// auto-bonus, which the real Grace skill tree (PrestigeSkillSystem) has now replaced.</summary>
        public double GraceEverSpent { get; private set; }

        /// <summary>How many times the player has prestiged via the free (no-reset) path.</summary>
        public int FreePrestigeCount { get; private set; }

        /// <summary>How many times the player has prestiged via the opt-in reset path - drives
        /// the permanent post-reset XP/income bonuses in GameLoopController and reset-gated skill
        /// tree nodes (2026-08-06). Never decreases, including on a later reset.</summary>
        public int ResetPrestigeCount { get; private set; }

        /// <summary>Combined total across both paths - kept for the one existing call site
        /// (ClickerScreenUI's "keep the Skill Tree button visible after any prestige" check) that
        /// only cares whether the player has EVER prestiged, not which kind.</summary>
        public int PrestigeCount => FreePrestigeCount + ResetPrestigeCount;

        /// <summary>floor(sqrt(TotalInkEarnedThisRun)/50) + 10 + floor(versesUnlocked/5) +
        /// chaptersCompleted*2 + booksCompleted*10 - blends genre-standard sqrt-dampened
        /// total-Ink-earned with content-completion terms (explicit user call after the
        /// genre-benchmarking pass, 2026-08-04).</summary>
        public static double CalculateGraceReward(double lifetimeInkEarned, int versesUnlocked, int chaptersCompleted, int booksCompleted)
        {
            double sqrtTerm = Math.Floor(Math.Sqrt(Math.Max(0, lifetimeInkEarned)) / 50.0);
            double verseTerm = Math.Floor(versesUnlocked / 5.0);
            return sqrtTerm + 10 + verseTerm + chaptersCompleted * 2 + booksCompleted * 10;
        }

        public void AwardGrace(double amount, bool withReset)
        {
            if (amount <= 0) return;
            Grace += amount;
            if (withReset) ResetPrestigeCount++;
            else FreePrestigeCount++;
        }

        public bool TrySpendGrace(double cost)
        {
            if (cost < 0 || Grace < cost) return false;
            Grace -= cost;
            GraceEverSpent += cost;
            return true;
        }

        /// <summary>Restores state from a save file (2026-08-08).</summary>
        public void LoadState(double savedGrace, double savedGraceEverSpent, int savedFreeCount, int savedResetCount)
        {
            Grace = Math.Max(0, savedGrace);
            GraceEverSpent = Math.Max(0, savedGraceEverSpent);
            FreePrestigeCount = Math.Max(0, savedFreeCount);
            ResetPrestigeCount = Math.Max(0, savedResetCount);
        }
    }
}
