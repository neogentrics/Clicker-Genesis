namespace ClickerGenesis.Data
{
    /// <summary>
    /// Single source of truth for canonical KJV Old Testament book order (Genesis first, then
    /// Exodus through Malachi) - shared between the Editor-only Grace Skill Tree generator
    /// (Assets/Editor/PrestigeSkillTreeGenerator.cs, which can't be referenced from runtime code)
    /// and runtime book-switching logic (GameLoopController, Phase F2, 2026-08-06), so both always
    /// agree on order without duplicating the list.
    /// </summary>
    public static class CanonicalBookOrder
    {
        /// <summary>Genesis first (always-free starting book, no Grace tree node), then every
        /// remaining OT book in canonical order, matching each book's Grace tree unlock node id
        /// ("Book_" + resourceId) and its Verses/ resource file name.</summary>
        public static readonly (string resourceId, string displayName)[] Books =
        {
            ("genesis_1", "Genesis"),
            ("exodus_2", "Exodus"), ("leviticus_3", "Leviticus"), ("numbers_4", "Numbers"),
            ("deuteronomy_5", "Deuteronomy"), ("joshua_6", "Joshua"), ("judges_7", "Judges"),
            ("ruth_8", "Ruth"), ("1samuel_9", "1 Samuel"), ("2samuel_10", "2 Samuel"),
            ("1kings_11", "1 Kings"), ("2kings_12", "2 Kings"), ("1chronicles_13", "1 Chronicles"),
            ("2chronicles_14", "2 Chronicles"), ("ezra_15", "Ezra"), ("nehemiah_16", "Nehemiah"),
            ("esther_17", "Esther"), ("job_18", "Job"), ("psalms_19", "Psalms"),
            ("proverbs_20", "Proverbs"), ("ecclesiastes_21", "Ecclesiastes"), ("songofsolomon_22", "Song of Solomon"),
            ("isaiah_23", "Isaiah"), ("jeremiah_24", "Jeremiah"), ("lamentations_25", "Lamentations"),
            ("ezekiel_26", "Ezekiel"), ("daniel_27", "Daniel"), ("hosea_28", "Hosea"),
            ("joel_29", "Joel"), ("amos_30", "Amos"), ("obadiah_31", "Obadiah"),
            ("jonah_32", "Jonah"), ("micah_33", "Micah"), ("nahum_34", "Nahum"),
            ("habakkuk_35", "Habakkuk"), ("zephaniah_36", "Zephaniah"), ("haggai_37", "Haggai"),
            ("zechariah_38", "Zechariah"), ("malachi_39", "Malachi"),
        };

        public static int IndexOf(string resourceId)
        {
            for (int i = 0; i < Books.Length; i++)
                if (Books[i].resourceId == resourceId) return i;
            return -1;
        }

        public static string DisplayNameOf(string resourceId)
        {
            int idx = IndexOf(resourceId);
            return idx >= 0 ? Books[idx].displayName : resourceId;
        }

        /// <summary>The resource id of the book immediately before this one in canonical order, or
        /// null if this IS the first book (Genesis) or resourceId isn't found.</summary>
        public static string PreviousResourceId(string resourceId)
        {
            int idx = IndexOf(resourceId);
            return idx > 0 ? Books[idx - 1].resourceId : null;
        }
    }
}
