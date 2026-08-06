namespace ClickerGenesis.Data
{
    /// <summary>
    /// Per-book verse-purchase progress - one instance per book the player has ever unlocked or
    /// touched. Introduced 2026-08-06 (Phase F, real book-switching) - before this,
    /// GameLoopController tracked a single global NextVerseIndex/chapter-gate/chapter-count as if
    /// only one book could ever exist. VerseDatabase itself stays stateless/reusable, unchanged -
    /// this just wraps one alongside the progress cursor into it.
    /// </summary>
    public class BookProgress
    {
        public readonly string ResourceId;
        public readonly string DisplayName;
        public VerseDatabase Database;

        public int NextVerseIndex;
        public int UnlockedChapterNumber = -1;
        public int ChaptersCompletedInBook;

        public BookProgress(string resourceId, string displayName, VerseDatabase database = null)
        {
            ResourceId = resourceId;
            DisplayName = displayName;
            Database = database;
        }

        /// <summary>A book with no verse data loaded yet has trivially not been completed - only
        /// meaningful once Database is actually populated (lazy-loaded on first activation).</summary>
        public bool IsComplete => Database != null && !Database.HasVerse(NextVerseIndex);
    }
}
