using System;
using System.Collections.Generic;

namespace ClickerGenesis.Data
{
    [Serializable]
    public class VerseEntry
    {
        public int verseNumber;
        public string text;
    }

    [Serializable]
    public class ChapterEntry
    {
        public int chapterNumber;
        public List<VerseEntry> verses;
    }

    [Serializable]
    public class BookEntry
    {
        public string bookName;
        public string translation;
        public List<ChapterEntry> chapters;
    }
}
