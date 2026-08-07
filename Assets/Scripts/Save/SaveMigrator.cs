namespace ClickerGenesis.Save
{
    /// <summary>
    /// Version-chain migration scaffolding (2026-08-08, per Save-System-Design.md §4). Only shape
    /// today is v1, so there's nothing to migrate FROM yet — this exists so the NEXT data-model
    /// change (which, per this project's own history — Phase F alone turned a single verse cursor
    /// into a per-book dictionary — WILL happen) has a real place to add a step instead of needing
    /// the whole save system retrofitted at that point.
    ///
    /// Pattern for adding a step later: bump CurrentVersion, add a MigrateVxToVy(SaveData) that
    /// fills in sensible defaults for whatever's new, and add it to the chain in Migrate() below.
    /// Each step should be a pure function - old shape in, new shape out, no side effects.
    /// </summary>
    public static class SaveMigrator
    {
        public const int CurrentVersion = 1;

        public static SaveData Migrate(SaveData data)
        {
            // No migrations exist yet - v1 is the only shape that has ever existed. When v2
            // happens:
            //   if (data.saveVersion < 2) data = MigrateV1ToV2(data);
            data.saveVersion = CurrentVersion;
            return data;
        }
    }
}
