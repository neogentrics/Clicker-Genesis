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
    }
}
