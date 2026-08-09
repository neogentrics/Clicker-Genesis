namespace ClickerGenesis.Save
{
    /// <summary>
    /// Version-chain migration scaffolding for AchievementData, same pattern as SaveMigrator - only
    /// shape today is v1, nothing to migrate from yet. Add a step here (not a parallel system) the
    /// day AchievementData's shape actually changes.
    /// </summary>
    public static class AchievementDataMigrator
    {
        public const int CurrentVersion = 1;

        public static AchievementData Migrate(AchievementData data)
        {
            data.dataVersion = CurrentVersion;
            return data;
        }
    }
}
