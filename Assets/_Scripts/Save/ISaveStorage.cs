namespace ClickerGenesis.Save
{
    /// <summary>
    /// Storage seam (2026-08-08, per Save-System-Design.md §8) so a future "layer 2" backend
    /// (cloud save, social-login-linked storage) can be added as a new implementation later
    /// without touching any of the call sites that read/write save data — those depend on this
    /// interface, not on file I/O directly.
    /// </summary>
    public interface ISaveStorage
    {
        SaveData Load();
        void Save(SaveData data);

        /// <summary>Removes any saved game from this storage (2026-08-08 - Settings screen's
        /// "Delete Saved Game" reset option). The next Load() should behave exactly like a fresh
        /// install.</summary>
        void DeleteSave();

        /// <summary>Cheap existence check (2026-08-08, save-slot system) - does a real save exist
        /// in this storage's slot, without paying the cost of a full Load()/deserialize just to
        /// find out. Used by the slot-picker screen to decide whether a slot shows "Continue" or
        /// "New Game" and whether Delete/Copy are meaningful for it.</summary>
        bool HasSave();
    }
}
