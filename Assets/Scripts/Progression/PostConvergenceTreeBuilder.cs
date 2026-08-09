using System.Collections.Generic;
using ClickerGenesis.Data;

namespace ClickerGenesis.Progression
{
    /// <summary>
    /// Builds the "Book Mastery" nodes - one per OT book, each requiring Convergence (all 8 economy
    /// branches maxed) PLUS that specific book being unlocked (2026-08-09, real user request/drawing:
    /// several branches merge into a single node, then re-split into a further set of nodes gated on
    /// having unlocked a book first, "kinda like this"). Built at runtime alongside
    /// BookProgressionTreeBuilder since it needs the same CanonicalBookOrder list; unlike Book
    /// Progression's generic player-chosen slots, these ARE tied to a specific book each (the whole
    /// point is "unlocking Exodus opens Exodus Mastery"), so there's no player-choice popup involved -
    /// PrestigeSkillSystem.IsUnlocked already handles the requiresBookResourceId gate generically.
    ///
    /// Deliberately uniform in effect (a flat, small +1% Ink/sec each) rather than 39 hand-invented
    /// unique bonuses - real per-book differentiated content is still explicitly deferred pending the
    /// user's own design pass (CLAUDE.md's standing "don't invent scripture-adjacent content
    /// unilaterally" rule), so this is the honest data-driven placeholder rather than pretend lore.
    /// </summary>
    public static class PostConvergenceTreeBuilder
    {
        private const double BaseCost = 400;
        private const double CostEscalationPerNode = 1.05;
        private const double EffectPerNode = 0.01;

        public static List<PrestigeSkillNode> Build()
        {
            var nodes = new List<PrestigeSkillNode>();
            var books = CanonicalBookOrder.Books;

            for (int i = 0; i < books.Length; i++)
            {
                var (id, name) = books[i];
                double cost = System.Math.Round(BaseCost * System.Math.Pow(CostEscalationPerNode, i));

                nodes.Add(new PrestigeSkillNode
                {
                    id = $"Mastery_{id}",
                    displayName = $"{name} Mastery",
                    description = $"Requires {name} unlocked. +{EffectPerNode * 100:0.#}% total Ink/sec, on top of every other bonus.",
                    branch = "Book Mastery",
                    prerequisites = new List<SkillPrerequisite> { new SkillPrerequisite("Convergence", 1) },
                    shape = SkillNodeShape.Hexagon,
                    maxRank = 1,
                    baseCost = cost,
                    costGrowthPerRank = 1.0,
                    effectType = SkillEffectType.IncomeMultiplier,
                    effectPerRank = EffectPerNode,
                    unlockBookResourceId = "",
                    unlockBookDisplayName = "",
                    isCapstone = false,
                    requiresResetPrestige = false,
                    requiresBookResourceId = id,
                });
            }

            return nodes;
        }
    }
}
