using System.Collections.Generic;
using ClickerGenesis.Data;

namespace ClickerGenesis.Progression
{
    /// <summary>
    /// Builds the Book Progression branch's nodes at RUNTIME - a single sequential chain of
    /// generic "Unlock New Book" slots, one per non-starting OT book, growing off Core just like
    /// any economy branch. Each slot's Grace cost still escalates with how deep into the chain it
    /// sits, but the slot no longer earmarks a SPECIFIC book at build time (2026-08-09 redesign,
    /// user's explicit ask - "make it so that when they unlock the next book, the skill says
    /// unlock new book... it gives them the option for a pop up to pick which book they unlock,
    /// especially if they pick a random book to start with"). Which book a slot actually grants is
    /// chosen by the player at purchase time (PrestigeScreenUI's book-choice popup,
    /// PrestigeSkillSystem.ChooseBook) - a random starting book no longer has to march through a
    /// fixed canonical neighbor order to reach the book the player actually wants next.
    ///
    /// This also drops the old two-arm (forward/backward from start) layout entirely - since a
    /// slot no longer has an inherent "distance" from any particular book, there's nothing left to
    /// split by direction. PrestigeScreenUI positions this branch exactly like an economy branch
    /// now (same wander-drift technique), which is a real simplification, not just a side effect.
    /// </summary>
    public static class BookProgressionTreeBuilder
    {
        private const double BookBaseCost = 30;
        private const double BookCostEscalationPerStep = 1.22;

        private const string GospelThresholdId = "Book_NT_Gospel Threshold";
        private const string ApostolicPathId = "Book_NT_Apostolic Path";
        private const string RevelationsVeilId = "Book_NT_Revelation's Veil";

        /// <summary>Builds one generic slot node per OT book except the starting book itself (which
        /// needs no node - it's already the player's book), in a single sequential chain, plus the
        /// 3-node New Testament gate cluster at the end of that chain. The startingBookResourceId
        /// parameter only decides the SLOT COUNT now (one fewer than the full 39 OT books) - it no
        /// longer anchors any per-slot book identity, so a random start doesn't distort the chain's
        /// shape the way the old distance-based version did.</summary>
        public static List<PrestigeSkillNode> Build(string startingBookResourceId)
        {
            var nodes = new List<PrestigeSkillNode>();
            var books = CanonicalBookOrder.Books;
            int slotCount = books.Length - 1; // every OT book except the one the player already has

            string previousId = "Core";
            for (int i = 1; i <= slotCount; i++)
            {
                string id = $"Book_Slot_{i}";
                double cost = System.Math.Round(BookBaseCost * System.Math.Pow(BookCostEscalationPerStep, i - 1));

                nodes.Add(new PrestigeSkillNode
                {
                    id = id,
                    displayName = "Unlock New Book",
                    description = "Choose any locked Old Testament book to unlock for purchase on the Books tab.",
                    branch = "Book Progression",
                    prerequisites = new List<SkillPrerequisite> { new SkillPrerequisite(previousId, 1) },
                    shape = SkillNodeShape.Diamond,
                    maxRank = 1,
                    baseCost = cost,
                    costGrowthPerRank = 1.0,
                    effectType = SkillEffectType.BookUnlock,
                    effectPerRank = 0,
                    unlockBookResourceId = "", // empty = generic, player chooses at purchase time
                    unlockBookDisplayName = "",
                    isCapstone = false,
                });

                previousId = id;
            }

            AddNewTestamentGate(nodes, previousId, slotCount);
            return nodes;
        }

        /// <summary>The New Testament gate now simply requires every generic OT slot to be owned
        /// (2026-08-09 - functionally equivalent to the old "reached both canonical ends" condition,
        /// since owning all 38 slots means the player has unlocked every OT book regardless of what
        /// order they picked them in).</summary>
        private static void AddNewTestamentGate(List<PrestigeSkillNode> nodes, string lastSlotId, int slotCount)
        {
            var afterAllBooks = slotCount > 0
                ? new List<SkillPrerequisite> { new SkillPrerequisite(lastSlotId, 1) }
                : new List<SkillPrerequisite> { new SkillPrerequisite("Core", 1) }; // degenerate 1-book edge case

            AddNtGateNode(nodes, GospelThresholdId, "Gospel Threshold", afterAllBooks,
                "matthew_40,mark_41,luke_42,john_43", 2000);
            AddNtGateNode(nodes, ApostolicPathId, "Apostolic Path", Single(GospelThresholdId),
                "acts_44,romans_45,1corinthians_46,2corinthians_47,galatians_48,ephesians_49,philippians_50,colossians_51,1thessalonians_52,2thessalonians_53,1timothy_54,2timothy_55,titus_56,philemon_57,hebrews_58,james_59,1peter_60,2peter_61,1john_62,2john_63,3john_64", 5000);
            AddNtGateNode(nodes, RevelationsVeilId, "Revelation's Veil", Single(ApostolicPathId),
                "jude_65,revelation_66", 8000);
        }

        private static List<SkillPrerequisite> Single(string nodeId) => new List<SkillPrerequisite> { new SkillPrerequisite(nodeId, 1) };

        private static void AddNtGateNode(List<PrestigeSkillNode> nodes, string nodeId, string displayName,
            List<SkillPrerequisite> prerequisites, string bookIds, double cost)
        {
            nodes.Add(new PrestigeSkillNode
            {
                id = nodeId,
                displayName = displayName,
                description = $"Unlocks New Testament books: {bookIds.Replace(",", ", ")}.",
                branch = "Book Progression",
                prerequisites = prerequisites,
                shape = SkillNodeShape.Diamond,
                maxRank = 1,
                baseCost = cost,
                costGrowthPerRank = 1.0,
                effectType = SkillEffectType.BookUnlock,
                effectPerRank = 0,
                unlockBookResourceId = bookIds, // static group-unlock - not player-chosen
                unlockBookDisplayName = displayName,
                isCapstone = displayName == "Revelation's Veil",
                requiresResetPrestige = true,
            });
        }
    }
}
