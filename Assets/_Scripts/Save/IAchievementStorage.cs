namespace ClickerGenesis.Save
{
    /// <summary>
    /// Storage seam for the global achievement ledger, mirroring ISaveStorage's role for slot
    /// saves - so a future "layer 2" backend (cloud sync, platform achievement services) can be
    /// added later without touching the AchievementSystem call sites, which depend on this
    /// interface, not file I/O directly.
    /// </summary>
    public interface IAchievementStorage
    {
        AchievementData Load();
        void Save(AchievementData data);

        /// <summary>Removes this storage's achievement ledger (2026-08-10 - achievements moved
        /// from a global ledger to per-save-slot, so a "Delete Saved Game" reset on a slot now
        /// wipes that slot's earned achievements too, same as it wipes everything else).</summary>
        void DeleteSave();
    }
}
