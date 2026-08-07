using System.Collections.Generic;
using UnityEngine;

namespace ClickerGenesis.Data
{
    /// <summary>
    /// Loads a book's verse JSON from Resources and exposes it as a flat, purchase-ordered
    /// sequence. Verse order within a book is canonical (chapter, then verse) — no skipping in v1.
    /// </summary>
    public class VerseDatabase
    {
        public readonly struct FlatVerse
        {
            public readonly int ChapterNumber;
            public readonly int VerseNumber;
            public readonly string Text;
            public readonly int IndexInBook;

            public FlatVerse(int chapterNumber, int verseNumber, string text, int indexInBook)
            {
                ChapterNumber = chapterNumber;
                VerseNumber = verseNumber;
                Text = text;
                IndexInBook = indexInBook;
            }

            public string Reference => $"{ChapterNumber}:{VerseNumber}";
        }

        public string BookName { get; private set; }
        public string Translation { get; private set; }

        private readonly List<FlatVerse> flatVerses = new List<FlatVerse>();

        public int VerseCount => flatVerses.Count;

        /// <param name="resourcePath">Path under a Resources folder, without extension, e.g. "Verses/genesis_1".</param>
        public static VerseDatabase LoadFromResources(string resourcePath)
        {
            var textAsset = Resources.Load<TextAsset>(resourcePath);
            if (textAsset == null)
            {
                Debug.LogError($"VerseDatabase: no TextAsset found at Resources/{resourcePath}.json");
                return null;
            }

            var book = JsonUtility.FromJson<BookEntry>(textAsset.text);
            var db = new VerseDatabase
            {
                BookName = book.bookName,
                Translation = book.translation
            };

            int index = 0;
            foreach (var chapter in book.chapters)
            {
                foreach (var verse in chapter.verses)
                {
                    db.flatVerses.Add(new FlatVerse(chapter.chapterNumber, verse.verseNumber, verse.text, index));
                    index++;
                }
            }

            return db;
        }

        public FlatVerse GetVerse(int indexInBook) => flatVerses[indexInBook];

        public bool HasVerse(int indexInBook) => indexInBook >= 0 && indexInBook < flatVerses.Count;

        /// <summary>Reverse lookup: chapter/verse -> flat book-relative index (2026-08-08, per
        /// CharacterIndex-Integration-Mapping.md §2) - needed to convert a CharacterIndex verse
        /// reference string like "Exodus 2:1" into the ScribeDefinition.unlockAtVerseIndex/
        /// managerUnlockAtVerseIndex this project actually stores. Linear scan is fine - this only
        /// runs at content-authoring time, never per-frame.</summary>
        public bool TryGetIndexOfVerse(int chapterNumber, int verseNumber, out int indexInBook)
        {
            for (int i = 0; i < flatVerses.Count; i++)
            {
                if (flatVerses[i].ChapterNumber == chapterNumber && flatVerses[i].VerseNumber == verseNumber)
                {
                    indexInBook = i;
                    return true;
                }
            }
            indexInBook = -1;
            return false;
        }
    }
}
