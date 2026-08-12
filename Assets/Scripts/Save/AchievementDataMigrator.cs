using System.Collections.Generic;
using System.Linq;

namespace ClickerGenesis.Save
{
    /// <summary>
    /// Version-chain migration scaffolding for AchievementData, same pattern as SaveMigrator.
    /// </summary>
    public static class AchievementDataMigrator
    {
        public const int CurrentVersion = 2;

        public static AchievementData Migrate(AchievementData data)
        {
            if (data.dataVersion < 2)
            {
                // v1 -> v2 (2026-08-10): unlockedIds (plain strings) -> unlocks (id + timestamp).
                // Real unlock time is unknown for anything earned before timestamps existed - left
                // blank rather than guessed, same "don't invent data" discipline as everywhere else.
                var existingIds = new HashSet<string>(data.unlocks.Select(u => u.id));
                foreach (var id in data.unlockedIds)
                    if (existingIds.Add(id))
                        data.unlocks.Add(new AchievementUnlockEntry { id = id, unlockedAtUtc = "" });
            }

            data.dataVersion = CurrentVersion;
            return data;
        }
    }
}
