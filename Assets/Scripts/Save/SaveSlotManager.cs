using System.Linq;
using ClickerGenesis.Data;
using UnityEngine;

namespace ClickerGenesis.Save
{
    /// <summary>
    /// Static coordinator for the 3-save-slot system (2026-08-08), per the "Major standing
    /// authorization" spec in CLAUDE.md - three independently save/load/delete/copy-able slots,
    /// each backed by its own LocalFileSaveStorage(slotIndex). Deliberately has nothing to do with
    /// AchievementSystem/AchievementFileStorage - that ledger is GLOBAL and lives outside the slot
    /// system entirely (see AchievementSystem's own doc comment for why).
    /// </summary>
    public static class SaveSlotManager
    {
        public const int SlotCount = 3;

        private const string CurrentSlotKey = "ActiveSaveSlot";

        /// <summary>Which slot GameLoopController should load/save against this session. Persisted
        /// across relaunches via PlayerPrefs (same pattern as GameSettings) purely so a future
        /// "resume where I left off" shortcut has somewhere to read from - the actual New
        /// Game/Continue flow always sets this explicitly before loading ClickerScreen, it never
        /// relies on this default silently carrying over from a previous session.</summary>
        public static int CurrentSlot
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt(CurrentSlotKey, 0), 0, SlotCount - 1);
            set { PlayerPrefs.SetInt(CurrentSlotKey, Mathf.Clamp(value, 0, SlotCount - 1)); PlayerPrefs.Save(); }
        }

        public readonly struct SlotSummary
        {
            public readonly int SlotIndex;
            public readonly bool HasSave;
            public readonly string ActiveBookDisplayName;
            public readonly int Level;
            public readonly int BooksCompleted;
            public readonly int TotalBooks;
            public readonly float CompletionPercent;

            /// <summary>Within-book progress for the card's second line (2026-08-08, per user
            /// request - the slot card previously only showed the OT-wide %, not how far into the
            /// CURRENT book the player actually is). Chapter/Verse reflect the last verse actually
            /// purchased (0/0 if the book hasn't been started yet); VersesOwned/VerseTotal give the
            /// fraction, BookProgressPercent the same thing as a percentage.</summary>
            public readonly int CurrentBookChapter;
            public readonly int CurrentBookVerseNumber;
            public readonly int CurrentBookVersesOwned;
            public readonly int CurrentBookVerseTotal;
            public readonly float CurrentBookProgressPercent;

            public SlotSummary(int slotIndex, bool hasSave, string activeBookDisplayName, int level,
                int booksCompleted, int totalBooks,
                int currentBookChapter = 0, int currentBookVerseNumber = 0,
                int currentBookVersesOwned = 0, int currentBookVerseTotal = 0)
            {
                SlotIndex = slotIndex;
                HasSave = hasSave;
                ActiveBookDisplayName = activeBookDisplayName;
                Level = level;
                BooksCompleted = booksCompleted;
                TotalBooks = totalBooks;
                CompletionPercent = totalBooks > 0 ? (float)booksCompleted / totalBooks * 100f : 0f;
                CurrentBookChapter = currentBookChapter;
                CurrentBookVerseNumber = currentBookVerseNumber;
                CurrentBookVersesOwned = currentBookVersesOwned;
                CurrentBookVerseTotal = currentBookVerseTotal;
                CurrentBookProgressPercent = currentBookVerseTotal > 0
                    ? (float)currentBookVersesOwned / currentBookVerseTotal * 100f : 0f;
            }

            public static SlotSummary Empty(int slotIndex) =>
                new SlotSummary(slotIndex, false, null, 0, 0, CanonicalBookOrder.Books.Length);
        }

        public static ISaveStorage StorageFor(int slotIndex) => new LocalFileSaveStorage(slotIndex);

        public static bool HasSave(int slotIndex) => StorageFor(slotIndex).HasSave();

        /// <summary>Reads a slot's save file directly off disk (NOT via the live GameLoopController
        /// - the slot-picker screen shows this before any slot's data is loaded into memory) and
        /// summarizes it for the slot-picker row. Loads each touched book's VerseDatabase to
        /// determine completion (cheap - only the handful of books a real save has actually
        /// touched, and this runs once per slot row, not per-frame).</summary>
        public static SlotSummary GetSummary(int slotIndex)
        {
            var storage = StorageFor(slotIndex);
            if (!storage.HasSave()) return SlotSummary.Empty(slotIndex);

            var data = storage.Load();
            if (string.IsNullOrEmpty(data.activeBookResourceId)) return SlotSummary.Empty(slotIndex);

            int booksCompleted = 0;
            int activeChapter = 0, activeVerseNumber = 0, activeVersesOwned = 0, activeVerseTotal = 0;
            foreach (var book in data.books)
            {
                var db = VerseDatabase.LoadFromResources($"Verses/{book.resourceId}");
                if (db == null) continue;
                if (book.nextVerseIndex >= db.VerseCount) booksCompleted++;

                if (book.resourceId == data.activeBookResourceId)
                {
                    activeVersesOwned = book.nextVerseIndex;
                    activeVerseTotal = db.VerseCount;
                    // Reflect the last verse actually purchased (matches what BuyVerseScreen itself
                    // displays) - index 0/verse 0 if nothing's been bought in this book yet.
                    int lastOwnedIndex = book.nextVerseIndex - 1;
                    if (lastOwnedIndex >= 0 && db.HasVerse(lastOwnedIndex))
                    {
                        var verse = db.GetVerse(lastOwnedIndex);
                        activeChapter = verse.ChapterNumber;
                        activeVerseNumber = verse.VerseNumber;
                    }
                }
            }

            string displayName = CanonicalBookOrder.DisplayNameOf(data.activeBookResourceId);
            return new SlotSummary(slotIndex, true, displayName, data.progression.currentLevel,
                booksCompleted, CanonicalBookOrder.Books.Length,
                activeChapter, activeVerseNumber, activeVersesOwned, activeVerseTotal);
        }

        public static void DeleteSlot(int slotIndex) => StorageFor(slotIndex).DeleteSave();

        /// <summary>Copies one slot's save into another, overwriting whatever was in the
        /// destination. Round-trips through Load()/Save() rather than a raw file copy so the
        /// destination file gets a fresh savedAtUtc/saveVersion stamp and its own .bak, same as any
        /// normal save write.</summary>
        public static void CopySlot(int fromSlot, int toSlot)
        {
            if (fromSlot == toSlot) return;
            var source = StorageFor(fromSlot);
            if (!source.HasSave()) return;
            StorageFor(toSlot).Save(source.Load());
        }

        public static SlotSummary[] GetAllSummaries() =>
            Enumerable.Range(0, SlotCount).Select(GetSummary).ToArray();
    }
}
