using System;
using System.Collections.Generic;
using System.Linq;
using ClickerGenesis.Data;
using ClickerGenesis.Economy;
using ClickerGenesis.Progression;
using ClickerGenesis.Save;
using UnityEngine;

namespace ClickerGenesis.Core
{
    /// <summary>
    /// Ties the tap -> Ink -> buy-next-verse loop together for a single active book.
    /// Minimal v1 scaffolding: one shared Ink wallet, canonical in-order verse purchase,
    /// no book switching / prestige yet.
    /// </summary>
    public class GameLoopController : MonoBehaviour
    {
        /// <summary>One book's scribe roster, wired in the Inspector alongside the book it belongs
        /// to (2026-08-08, multi-book economy) - additionalScribeConfigs holds every book beyond
        /// Genesis (which keeps its own dedicated scribeConfig field for backwards Inspector
        /// compatibility).</summary>
        [System.Serializable]
        public class BookScribeConfig
        {
            public string bookResourceId;
            public ScribeSetConfig config;
        }

        [Header("Config")]
        [SerializeField] private VersePricingConfig pricingConfig;
        [SerializeField] private XpConfig xpConfig;
        [SerializeField] private ScribeSetConfig scribeConfig;
        [SerializeField] private List<BookScribeConfig> additionalScribeConfigs = new List<BookScribeConfig>();
        [SerializeField] private string verseResourcePath = "Verses/genesis_1";
        [SerializeField] private double tapAmount = 1;
        [SerializeField] private double startingInk = 0;

        [Header("Click Power upgrade")]
        [SerializeField] private double clickPowerBaseCost = 25;
        [SerializeField] private double clickPowerCostGrowthRate = 1.15;

        [Header("Grace skill tree")]
        [SerializeField] private PrestigeSkillTreeConfig prestigeSkillTreeConfig;

        public InkWallet Wallet { get; private set; }
        public LevelSystem Levels { get; private set; }

        /// <summary>One ScribeSystem per book that actually has a roster authored (2026-08-08,
        /// multi-book economy - was a single instance when only Genesis existed). Every registered
        /// book's scribes keep producing Ink in the background regardless of which is active
        /// (explicit user decision, 2026-08-08) - see EffectiveInkPerSecond, which sums across all
        /// of these, not just the active one.</summary>
        private readonly Dictionary<string, ScribeSystem> scribeSystemsByBook = new Dictionary<string, ScribeSystem>();

        /// <summary>The ACTIVE book's scribe roster - what Scribes/Managers/Support tabs display
        /// and what BuyScribe/BuyManager/BuySubmanager operate on. Null if the active book has no
        /// roster authored yet.</summary>
        public ScribeSystem Scribes =>
            scribeSystemsByBook.TryGetValue(activeBookResourceId, out var s) ? s : null;

        public PrestigeSystem Prestige { get; private set; }
        public PrestigeSkillSystem Skills { get; private set; }
        public PrestigeSkillTreeConfig SkillTreeConfig => prestigeSkillTreeConfig;

        // ---------- Per-book progress (2026-08-06, Phase F) ----------
        // Every unlocked/touched book gets its own BookProgress record (verse cursor, chapter
        // gate, per-book chapter count) instead of the old single global fields - see
        // BookProgress.cs. Genesis' resource id is derived from the existing verseResourcePath
        // field so nothing about the inspector-configured starting book changes.
        private readonly Dictionary<string, BookProgress> bookProgress = new Dictionary<string, BookProgress>();
        private string activeBookResourceId;
        private string GenesisResourceId => StripVersesPrefix(verseResourcePath);

        private static string StripVersesPrefix(string resourcePath) =>
            resourcePath.StartsWith("Verses/") ? resourcePath.Substring("Verses/".Length) : resourcePath;

        private BookProgress ActiveBook =>
            bookProgress.TryGetValue(activeBookResourceId, out var bp) ? bp : null;

        // ---------- Save/load (2026-08-08, per Save-System-Design.md) ----------
        private ISaveStorage saveStorage;
        private float autoSaveTimer;
        private const float AutoSaveIntervalSeconds = 30f;

        /// <summary>Superseded 2026-08-08 (multi-book economy): scribe/manager gating used to
        /// always read Genesis' progress specifically regardless of the active book, since only
        /// Genesis had a roster and the Scribes/Managers/Support tabs needed to keep showing it
        /// correctly no matter which book was active. Now that every book gets its OWN roster and
        /// the tabs only ever display the ACTIVE book's roster (explicit user decision,
        /// 2026-08-08), gating against NextVerseIndex/Verses (the active-book proxies below) is
        /// simply correct again - Genesis' scribes only ever need Genesis' progress because they
        /// only ever show while Genesis is active.</summary>
        public VerseDatabase Verses => ActiveBook?.Database;

        /// <summary>Index (within the ACTIVE book) of the next verse that has not yet been
        /// purchased - proxies to the active book's BookProgress record (2026-08-06).</summary>
        public int NextVerseIndex => ActiveBook?.NextVerseIndex ?? 0;

        /// <summary>Sum of chapters completed across every book ever touched (2026-08-06) - one of
        /// the Grace reward formula's terms. Computed, not a separately-incremented field, so it
        /// can never drift out of sync with the per-book records it's derived from.</summary>
        public int ChaptersCompletedCount => bookProgress.Values.Sum(b => b.ChaptersCompletedInBook);

        /// <summary>Sum of books fully completed across every book ever touched (2026-08-06) - the
        /// last Grace reward term. Computed from each BookProgress.IsComplete, same
        /// never-drifts-out-of-sync reasoning as ChaptersCompletedCount above.</summary>
        public int BooksCompletedCount => bookProgress.Values.Count(b => b.IsComplete);

        /// <summary>Every OT book in canonical order (Genesis first) - for the Books tab (Phase F3).</summary>
        public IReadOnlyList<(string resourceId, string displayName)> AllBooksInOrder => CanonicalBookOrder.Books;

        /// <summary>Fraction (0-1) of the ACTIVE book's verses bought so far - for the triple-lane
        /// progress bar's "current book" lane (2026-08-08). Uses the active book's own loaded
        /// VerseDatabase.VerseCount rather than a hardcoded table, since it's already in memory.</summary>
        public float ActiveBookProgressFraction =>
            Verses != null && Verses.VerseCount > 0 ? Mathf.Clamp01((float)NextVerseIndex / Verses.VerseCount) : 0f;

        /// <summary>Full KJV Old Testament verse count (Genesis-Malachi), confirmed against
        /// kjv_outline.json - see CLAUDE.md's "Full canonical KJV outline" note. A fixed constant
        /// rather than summing per-book counts at runtime, since most books' VerseDatabase isn't
        /// loaded until the player actually switches into them.</summary>
        private const int OldTestamentTotalVerses = 31102;

        /// <summary>Fraction (0-1) of the full Old Testament bought so far, summed across every
        /// book ever touched (2026-08-08) - for the triple-lane progress bar's bottom "overall OT
        /// progress" lane. Untouched books simply contribute 0, which is correct.</summary>
        public float OldTestamentProgressFraction =>
            Mathf.Clamp01(bookProgress.Values.Sum(b => b.NextVerseIndex) / (float)OldTestamentTotalVerses);

        public string ActiveBookResourceId => activeBookResourceId;

        public bool IsBookActive(string resourceId) => resourceId == activeBookResourceId;

        public bool IsBookComplete(string resourceId) =>
            bookProgress.TryGetValue(resourceId, out var b) && b.IsComplete;

        /// <summary>True if resourceId can become the active book right now: unlocked via the
        /// Grace tree (or it's Genesis, always free) AND the immediately-preceding canonical book
        /// is fully complete (2026-08-06, Phase F2 - "can't start the next book until the first
        /// book is finished"). A book with no BookProgress record yet (never touched) fails the
        /// previous-book check rather than throwing - not started means not complete.</summary>
        public bool CanSwitchToBook(string resourceId)
        {
            if (string.IsNullOrEmpty(resourceId)) return false;

            bool isUnlocked = resourceId == GenesisResourceId || (Skills != null && Skills.IsBookUnlocked(resourceId));
            if (!isUnlocked) return false;

            string previousId = CanonicalBookOrder.PreviousResourceId(resourceId);
            if (previousId == null) return true; // Genesis - no previous book required

            return bookProgress.TryGetValue(previousId, out var previous) && previous.IsComplete;
        }

        /// <summary>Switches the active book to resourceId, lazy-loading its VerseDatabase on
        /// first activation and creating its BookProgress record if this is the first time it's
        /// ever been switched to. Returns false (no state change) if CanSwitchToBook says no.</summary>
        public bool SwitchActiveBook(string resourceId)
        {
            if (!CanSwitchToBook(resourceId)) return false;
            if (resourceId == activeBookResourceId) return true; // already active, no-op success

            if (!bookProgress.TryGetValue(resourceId, out var progress))
            {
                progress = new BookProgress(resourceId, CanonicalBookOrder.DisplayNameOf(resourceId));
                bookProgress[resourceId] = progress;
            }
            if (progress.Database == null)
                progress.Database = VerseDatabase.LoadFromResources($"Verses/{resourceId}");

            activeBookResourceId = resourceId;
            OnStateChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// How many times the player has bought a Click Power upgrade. Tap value scales via the
        /// same 10/25/50/100-purchase milestone curve as scribe output (see MilestoneCurve) —
        /// "instead of each click being worth 1, it's worth 1.2x at 10, 2x at 25, 3x at 50..."
        /// as originally requested. Placeholder cost numbers, pending playtesting.
        /// </summary>
        public int ClickPowerLevel { get; private set; }

        public double ClickPowerCost => clickPowerBaseCost * Math.Pow(clickPowerCostGrowthRate, ClickPowerLevel);

        /// <summary>
        /// Milestone-only scaling (1x/1.2x/2x/3x/4x at 10/25/50/100 owned) left every purchase
        /// before the next breakpoint with zero visible effect - bought 1 through 9 did nothing,
        /// which read as broken. A flat +10%-per-level compounds on top of the milestone jumps so
        /// every single purchase visibly moves the number, with the milestone breakpoints still
        /// landing as bigger jumps.
        /// </summary>
        private const double ClickPowerPerLevelGrowth = 0.1;

        public double EffectiveTapAmount =>
            tapAmount * (1.0 + ClickPowerPerLevelGrowth * ClickPowerLevel) * MilestoneCurve.GetMultiplier(ClickPowerLevel)
            * (1.0 + Skills.GetTotalEffect(SkillEffectType.ClickPowerMultiplier));

        /// <summary>What EffectiveTapAmount becomes after the next Click Power purchase — used to
        /// preview the upgrade's payoff on its button label instead of showing only its cost.</summary>
        public double NextEffectiveTapAmount =>
            tapAmount * (1.0 + ClickPowerPerLevelGrowth * (ClickPowerLevel + 1)) * MilestoneCurve.GetMultiplier(ClickPowerLevel + 1)
            * (1.0 + Skills.GetTotalEffect(SkillEffectType.ClickPowerMultiplier));

        [Header("Bulk buy")]
        private static readonly int[] VerseMultiplierTiers = { 1, 5, 10, 20, MaxBuyMultiplier };

        /// <summary>-1 is the "Max" sentinel (2026-08-04, explicit user request alongside the
        /// auto-buy toggle) - buys as many as the wallet can afford in one click instead of a
        /// fixed count. Kept in the same tier-cycling array/int field as the fixed multipliers so
        /// every call site (ScribeBulkCost, BuyScribeBulk, Click Power) only needed a Max-aware
        /// branch, not a parallel code path.</summary>
        public const int MaxBuyMultiplier = -1;
        private static readonly int[] ScribeMultiplierTiers = { 1, 5, 10, 20, 100, MaxBuyMultiplier };

        /// <summary>How many scribes of a tier "Buy" purchases per click. Cycles 1/5/10/20/100/Max,
        /// same tiers as Click Power - bug #26, scribes never got a bulk-buy option like Verses
        /// and Click Power did.</summary>
        public int ScribeBuyMultiplier { get; private set; } = 1;

        public void CycleScribeBuyMultiplier()
        {
            int idx = (System.Array.IndexOf(ScribeMultiplierTiers, ScribeBuyMultiplier) + 1) % ScribeMultiplierTiers.Length;
            ScribeBuyMultiplier = ScribeMultiplierTiers[idx];
            OnStateChanged?.Invoke();
        }

        /// <summary>Whether managers auto-purchase their tier once affordable (mirrors
        /// GameSettings.ManagerAutoBuyEnabled so UI can read/toggle it without a static reference).</summary>
        public bool ManagerAutoBuyEnabled => GameSettings.ManagerAutoBuyEnabled;

        public void ToggleManagerAutoBuy()
        {
            GameSettings.ManagerAutoBuyEnabled = !GameSettings.ManagerAutoBuyEnabled;
            OnStateChanged?.Invoke();
        }

        public double ManagerAutoBuyReserve => GameSettings.ManagerAutoBuyReserve;

        public void CycleManagerAutoBuyReserve()
        {
            int next = (GameSettings.ManagerAutoBuyReserveIndex + 1) % GameSettings.ManagerAutoBuyReserveTiers.Length;
            GameSettings.ManagerAutoBuyReserveIndex = next;
            OnStateChanged?.Invoke();
        }

        /// <summary>How many verses "Buy Next Verse" purchases per click. Cycles 1/5/10/20 - 20 is
        /// the max, per explicit request ("I'm not going higher than that").</summary>
        public int VerseBuyMultiplier { get; private set; } = 1;

        /// <summary>How many Click Power upgrades "Upgrade Tap" buys per click. Unified
        /// (2026-08-04) with ScribeBuyMultiplier per explicit request - one multiplier control
        /// (the Scribes tab's "Multiplier" button) now drives both scribe and Click Power bulk-buy,
        /// instead of two separate cycle buttons doing the same conceptual thing.</summary>
        public int ClickPowerBuyMultiplier => ScribeBuyMultiplier;

        public void CycleVerseBuyMultiplier()
        {
            int idx = (System.Array.IndexOf(VerseMultiplierTiers, VerseBuyMultiplier) + 1) % VerseMultiplierTiers.Length;
            VerseBuyMultiplier = VerseMultiplierTiers[idx];
            OnStateChanged?.Invoke();
        }

        /// <summary>How many verses the current wallet balance can afford in a row, capped at the
        /// current chapter's boundary - Verse counterpart to MaxAffordableScribeCount/
        /// MaxAffordableClickPowerCount.</summary>
        private int MaxAffordableVerseCount()
        {
            if (RequiresChapterUnlock) return 0;
            double remaining = Wallet.Balance;
            int cap = RemainingVersesInCurrentChapter;
            int count = 0;
            for (int i = 0; i < cap; i++)
            {
                double cost = VerseCostAt(NextVerseIndex + i);
                if (cost > remaining) break;
                remaining -= cost;
                count++;
            }
            return count;
        }

        /// <summary>Total cost to buy VerseBuyMultiplier verses starting from NextVerseIndex,
        /// capped at the current chapter's boundary (2026-08-04 - verse purchases, single or bulk,
        /// never cross into a fresh chapter; only BuyNextChapter can do that). Zero while
        /// RequiresChapterUnlock is true. MaxBuyMultiplier resolves to however many the wallet can
        /// afford within the current chapter.</summary>
        public double VerseBulkCost
        {
            get
            {
                if (RequiresChapterUnlock) return 0;
                int count = VerseBuyMultiplier == MaxBuyMultiplier
                    ? MaxAffordableVerseCount()
                    : Math.Min(VerseBuyMultiplier, RemainingVersesInCurrentChapter);
                double total = 0;
                for (int i = 0; i < count; i++)
                    total += VerseCostAt(NextVerseIndex + i);
                return total;
            }
        }

        /// <summary>How many Click Power upgrades the current wallet balance can afford in a row -
        /// Click Power counterpart to MaxAffordableScribeCount, same Max-multiplier reasoning.</summary>
        private int MaxAffordableClickPowerCount()
        {
            double remaining = Wallet.Balance;
            int count = 0;
            while (true)
            {
                double cost = clickPowerBaseCost * Math.Pow(clickPowerCostGrowthRate, ClickPowerLevel + count);
                if (cost > remaining) break;
                remaining -= cost;
                count++;
            }
            return count;
        }

        /// <summary>Total cost to buy ClickPowerBuyMultiplier Click Power upgrades in a row.
        /// MaxBuyMultiplier resolves to however many the current wallet balance can afford.</summary>
        public double ClickPowerBulkCost
        {
            get
            {
                int count = ClickPowerBuyMultiplier == MaxBuyMultiplier ? MaxAffordableClickPowerCount() : ClickPowerBuyMultiplier;
                double total = 0;
                for (int i = 0; i < count; i++)
                    total += clickPowerBaseCost * Math.Pow(clickPowerCostGrowthRate, ClickPowerLevel + i);
                return total;
            }
        }

        /// <summary>What EffectiveTapAmount becomes after buying ClickPowerBuyMultiplier upgrades
        /// in one go - previews the bulk purchase's payoff, not just the next single one.</summary>
        public double ClickPowerBulkPreviewTapAmount
        {
            get
            {
                int afterLevel = ClickPowerLevel + ClickPowerBuyMultiplier;
                return tapAmount * (1.0 + ClickPowerPerLevelGrowth * afterLevel) * MilestoneCurve.GetMultiplier(afterLevel)
                    * (1.0 + Skills.GetTotalEffect(SkillEffectType.ClickPowerMultiplier));
            }
        }

        public event Action OnStateChanged;

        /// <summary>
        /// Persistent singleton — spawned once in the Main Menu scene, survives every
        /// subsequent scene load so Ink/XP/unlocked-verse progress isn't lost when the
        /// player navigates between screens. No save/load system yet (explicitly deferred);
        /// this only keeps state alive for the current play session, in memory.
        /// </summary>
        public static GameLoopController Instance { get; private set; }

        /// <summary>
        /// Every non-MainMenu screen's Awake() should call this first. GameRoot (and everything
        /// on it — Wallet, Scribes, EventSystem) only spawns in MainMenu.unity; if the Editor
        /// happens to be sitting on any other scene when Play is pressed (easy to do by accident
        /// after navigating scenes while working), that screen loads with no GameLoopController
        /// at all — nothing works, silently. This redirects to MainMenu instead of failing quiet.
        /// </summary>
        public static bool EnsureBootstrapped()
        {
            if (Instance != null) return true;
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
            return false;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            // AddComponent triggers Awake() immediately even in Editor scripts (outside Play
            // mode), where DontDestroyOnLoad throws. Guard it — Awake() re-runs for real when
            // Play mode actually starts, so this still persists correctly at runtime.
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);

            BuildFreshState();

            if (Application.isPlaying) GameSettings.ApplyDisplaySettings();

            // Save/load (2026-08-08, per Save-System-Design.md) - LoadGame() overwrites the
            // just-built default/fresh state above ONLY if a real save exists on disk; a fresh
            // install's SaveData comes back with an empty activeBookResourceId, which LoadGame()
            // treats as "nothing to apply" so a brand-new player still starts at Genesis verse 0.
            if (Application.isPlaying)
            {
                saveStorage = new LocalFileSaveStorage();
                LoadGame();
            }
        }

        /// <summary>Builds the same brand-new-player default state Awake() has always built -
        /// factored out (2026-08-08) so the Settings screen's "Delete Saved Game" reset option can
        /// reuse it directly instead of duplicating this logic. Wipes every in-memory system back
        /// to a fresh Genesis-at-verse-0 start; does NOT touch disk (ResetGameAndDeleteSave below
        /// is the caller responsible for actually deleting the save file first).</summary>
        private void BuildFreshState()
        {
            Wallet = new InkWallet(startingInk);
            Levels = new LevelSystem(xpConfig);
            scribeSystemsByBook.Clear();
            if (scribeConfig != null) scribeSystemsByBook[GenesisResourceId] = new ScribeSystem(scribeConfig);
            foreach (var entry in additionalScribeConfigs)
                if (entry.config != null && !string.IsNullOrEmpty(entry.bookResourceId))
                    scribeSystemsByBook[entry.bookResourceId] = new ScribeSystem(entry.config);
            Prestige = new PrestigeSystem();
            Skills = new PrestigeSkillSystem(prestigeSkillTreeConfig);

            // Genesis is always the hardcoded starting book (2026-08-06) - seeded into
            // bookProgress exactly like the old single-book init, just wrapped in a BookProgress
            // record now so other books can get their own records alongside it.
            bookProgress.Clear();
            activeBookResourceId = GenesisResourceId;
            var genesisDb = VerseDatabase.LoadFromResources(verseResourcePath);
            bookProgress[activeBookResourceId] = new BookProgress(activeBookResourceId, "Genesis", genesisDb);

            ClickPowerLevel = 0;
            ProgressMultiplier = 1f;
        }

        /// <summary>Settings screen's "Delete Saved Game" option (2026-08-08, explicit user ask -
        /// "if the game is corrupt or something went wrong, we can undo it," on top of the plain
        /// full-restart use case). Deletes the on-disk save FIRST, then rebuilds in-memory state to
        /// the same fresh-install shape Awake() would produce - so even if the caller's own
        /// subsequent scene load to MainMenu were somehow skipped, the live game state is already
        /// correct, not just the file on disk.</summary>
        public void ResetGameAndDeleteSave()
        {
            saveStorage?.DeleteSave();
            BuildFreshState();
            autoSaveTimer = 0f;
            OnStateChanged?.Invoke();
        }

        private void Update()
        {
            if (scribeSystemsByBook.Count == 0) return;

            bool changed = false;

            double inkPerSecond = EffectiveInkPerSecond;
            if (inkPerSecond > 0)
            {
                Wallet.Add(inkPerSecond * Time.deltaTime);
                changed = true;
            }

            // Manager auto-buy (2026-08-04): a manager doesn't just boost its tier's output — once
            // bought, it also auto-purchases that tier itself whenever affordable, same as
            // AdVenture Capitalist-style managers. Plain arithmetic per tier per frame (no
            // allocations, no UI work) — this is not the kind of per-frame cost that caused the
            // forced-UI-rebuild lag in bug #22. Opt-out toggle + a spendable-reserve floor added
            // per the user's follow-up request once they'd seen it always-on for a while.
            // Loops every registered book's roster (2026-08-08, multi-book economy), not just the
            // active one - a manager keeps auto-buying for a book you've switched away from, same
            // "scribes keep working in the background" rule EffectiveInkPerSecond follows.
            if (GameSettings.ManagerAutoBuyEnabled)
            {
                double reserve = GameSettings.ManagerAutoBuyReserve;
                foreach (var kvp in scribeSystemsByBook)
                {
                    var sys = kvp.Value;
                    int verseIndex = VerseIndexForBook(kvp.Key);
                    for (int i = 0; i < sys.TierCount; i++)
                    {
                        if (!sys.IsManagerUnlocked(i)) continue;
                        if (!sys.IsUnlocked(i, verseIndex)) continue;

                        double cost = sys.GetNextCost(i);
                        if (Wallet.Balance - cost < reserve) continue;
                        if (!Wallet.TrySpend(cost)) continue;

                        sys.Buy(i);
                        changed = true;
                    }
                }
            }

            if (changed) OnStateChanged?.Invoke();

            // Debounced periodic save (2026-08-08) - on top of the forced synchronous saves in
            // OnApplicationPause/OnApplicationQuit below. §6's reasoning: save-on-every-change is
            // the most robust option but the most I/O; a throttled interval is the safer default
            // for a mobile target, since a desktop session that's just idling with the window
            // open still wants SOME periodic safety net beyond "only saves when backgrounded."
            if (saveStorage != null)
            {
                autoSaveTimer += Time.deltaTime;
                if (autoSaveTimer >= AutoSaveIntervalSeconds)
                {
                    autoSaveTimer = 0f;
                    SaveGame();
                }
            }
        }

        /// <summary>Android has no guaranteed quit event - OnApplicationPause(true) is the last
        /// reliable hook before the OS may kill a backgrounded process, so it forces an immediate
        /// synchronous save rather than waiting for the next debounced autosave tick (§6).</summary>
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) SaveGame();
        }

        private void OnApplicationQuit()
        {
            SaveGame();
        }

        /// <summary>Writes the current live state to disk via saveStorage. Safe to call anytime
        /// after Awake() - no-ops if saveStorage hasn't been set up yet (e.g. called from an
        /// editor-time context outside Play mode).</summary>
        public void SaveGame()
        {
            if (saveStorage == null) return;
            saveStorage.Save(CaptureSaveData());
        }

        private void LoadGame()
        {
            var data = saveStorage.Load();
            // A fresh install's SaveData comes back with activeBookResourceId still at its C#
            // default (null/empty) - that's the signal to leave Awake()'s just-built default
            // state alone rather than applying an empty save over it.
            if (string.IsNullOrEmpty(data.activeBookResourceId)) return;

            ApplySaveData(data);
            OnStateChanged?.Invoke();
        }

        /// <summary>Builds a SaveData snapshot of the current live state - the save-file
        /// counterpart to ApplySaveData below. Walks every system's own public state rather than
        /// duplicating values, so this can't silently drift from what SaveGame actually persists.</summary>
        public SaveData CaptureSaveData()
        {
            var data = new SaveData();

            data.economy.inkBalance = Wallet.Balance;
            data.economy.inkLifetimeEarned = Wallet.LifetimeEarned;
            data.economy.inkTotalSpent = Wallet.TotalSpent;
            data.economy.clickPowerLevel = ClickPowerLevel;
            data.economy.progressMultiplier = ProgressMultiplier;
            foreach (var kvp in scribeSystemsByBook)
            {
                var sys = kvp.Value;
                var bookState = new BookScribeState { bookResourceId = kvp.Key };
                bookState.owned.AddRange(sys.ExportOwned());
                bookState.managerUnlocked.AddRange(sys.ExportManagerUnlocked());
                foreach (var tierSubs in sys.ExportSubmanagerOwned())
                    bookState.submanagerOwned.Add(new TierSubmanagerState { owned = new List<bool>(tierSubs) });
                data.economy.scribeBooks.Add(bookState);
            }

            data.progression.totalXp = Levels.TotalXp;
            data.progression.currentLevel = Levels.CurrentLevel;

            data.prestige.grace = Prestige.Grace;
            data.prestige.graceEverSpent = Prestige.GraceEverSpent;
            data.prestige.freePrestigeCount = Prestige.FreePrestigeCount;
            data.prestige.resetPrestigeCount = Prestige.ResetPrestigeCount;
            foreach (var kvp in Skills.ExportRanks())
                data.prestige.skillRanks.Add(new SkillRankEntry { nodeId = kvp.Key, rank = kvp.Value });

            foreach (var kvp in bookProgress)
            {
                var bp = kvp.Value;
                data.books.Add(new BookProgressEntry
                {
                    resourceId = bp.ResourceId,
                    nextVerseIndex = bp.NextVerseIndex,
                    unlockedChapterNumber = bp.UnlockedChapterNumber,
                    chaptersCompletedInBook = bp.ChaptersCompletedInBook
                });
            }
            data.activeBookResourceId = activeBookResourceId;

            return data;
        }

        /// <summary>Restores live state from a loaded save file - the counterpart to
        /// CaptureSaveData above. A book referenced in the save but not yet touched this session
        /// (i.e. not Genesis) gets a fresh BookProgress record created and its VerseDatabase
        /// lazy-loaded, same as SwitchActiveBook's own lazy-load path.</summary>
        private void ApplySaveData(SaveData data)
        {
            Wallet.LoadState(data.economy.inkBalance, data.economy.inkLifetimeEarned, data.economy.inkTotalSpent);
            ClickPowerLevel = data.economy.clickPowerLevel;
            ProgressMultiplier = data.economy.progressMultiplier;

            foreach (var bookState in data.economy.scribeBooks)
            {
                if (!scribeSystemsByBook.TryGetValue(bookState.bookResourceId, out var sys)) continue;
                var submanagerOwned = new bool[bookState.submanagerOwned.Count][];
                for (int i = 0; i < bookState.submanagerOwned.Count; i++)
                    submanagerOwned[i] = bookState.submanagerOwned[i].owned.ToArray();
                sys.ImportState(bookState.owned.ToArray(), bookState.managerUnlocked.ToArray(), submanagerOwned);
            }

            Levels.LoadState(data.progression.totalXp, data.progression.currentLevel);

            Prestige.LoadState(data.prestige.grace, data.prestige.graceEverSpent,
                data.prestige.freePrestigeCount, data.prestige.resetPrestigeCount);
            Skills.LoadState(data.prestige.skillRanks.Select(e => new KeyValuePair<string, int>(e.nodeId, e.rank)));

            foreach (var entry in data.books)
            {
                if (!bookProgress.TryGetValue(entry.resourceId, out var bp))
                {
                    bp = new BookProgress(entry.resourceId, CanonicalBookOrder.DisplayNameOf(entry.resourceId));
                    bookProgress[entry.resourceId] = bp;
                }
                if (bp.Database == null)
                    bp.Database = VerseDatabase.LoadFromResources($"Verses/{entry.resourceId}");
                bp.NextVerseIndex = entry.nextVerseIndex;
                bp.UnlockedChapterNumber = entry.unlockedChapterNumber;
                bp.ChaptersCompletedInBook = entry.chaptersCompletedInBook;
            }

            if (!string.IsNullOrEmpty(data.activeBookResourceId) && bookProgress.ContainsKey(data.activeBookResourceId))
                activeBookResourceId = data.activeBookResourceId;
        }

        /// <summary>Verse progress for a specific book's own BookProgress record, independent of
        /// which book is currently active (2026-08-08) - lets a non-active book's scribe system
        /// still gate correctly against its own progress (manager auto-buy, background income).</summary>
        private int VerseIndexForBook(string bookResourceId) =>
            bookProgress.TryGetValue(bookResourceId, out var bp) ? bp.NextVerseIndex : 0;

        /// <summary>Attempts to buy one more of a scribe tier. Returns false if unaffordable, locked, or config missing.</summary>
        public bool BuyScribe(int tierIndex)
        {
            if (Scribes == null) return false;
            if (!Scribes.IsUnlocked(tierIndex, NextVerseIndex)) return false;

            double cost = Scribes.GetNextCost(tierIndex);
            if (!Wallet.TrySpend(cost)) return false;

            Scribes.Buy(tierIndex);
            // Buying a scribe grants 0 XP normally - only starts granting XP once the player has
            // performed at least one Reset-Prestige (2026-08-06, user's explicit ask).
            if (xpConfig != null && Prestige.ResetPrestigeCount > 0)
                Levels.AddXp(xpConfig.XpPerScribePurchaseAfterReset);
            OnStateChanged?.Invoke();
            return true;
        }

        /// <summary>Attempts to unlock a tier's manager - requires the level threshold, the
        /// manager's own scribe tier being verse-unlocked (see ScribeSystem.CanUnlockManager), AND
        /// spending its Ink cost (0 for Adam, the free first manager). Returns false if not
        /// eligible, already unlocked, or unaffordable.</summary>
        /// <summary>Player level as seen by manager unlock checks - the Grace skill tree's
        /// "Manager's Calling" branch lowers the effective requirement (never below 1), rather than
        /// raising the player's real level.</summary>
        public int EffectiveManagerLevel =>
            Math.Max(1, Levels.CurrentLevel + (int)Skills.GetTotalEffect(SkillEffectType.ManagerUnlockLevelDiscount));

        public bool BuyManager(int tierIndex)
        {
            if (Scribes == null || !Scribes.CanUnlockManager(tierIndex, EffectiveManagerLevel, NextVerseIndex)) return false;

            double cost = Scribes.GetDefinition(tierIndex).managerUnlockCost;
            if (cost > 0 && !Wallet.TrySpend(cost)) return false;

            Scribes.UnlockManager(tierIndex);
            OnStateChanged?.Invoke();
            return true;
        }

        /// <summary>Hires a submanager (2026-08-06) - gated on the character's own real
        /// first-mention verse within their book (the active book's NextVerseIndex, 2026-08-08 -
        /// see the multi-book economy note on Verses above for why this is correct now).</summary>
        public bool BuySubmanager(int tierIndex, int subIndex)
        {
            if (Scribes == null || !Scribes.CanBuySubmanager(tierIndex, subIndex, NextVerseIndex, EffectiveManagerLevel)) return false;

            double cost = Scribes.GetSubmanagerDefinition(tierIndex, subIndex).unlockCost;
            if (cost > 0 && !Wallet.TrySpend(cost)) return false;

            Scribes.BuySubmanager(tierIndex, subIndex);
            OnStateChanged?.Invoke();
            return true;
        }

        /// <summary>How many of a tier the current wallet balance can afford in a row, walking the
        /// cost curve from the tier's current owned count. Shared by ScribeBulkCost/BuyScribeBulk's
        /// Max-multiplier branch so the displayed cost and the actual purchase agree.</summary>
        private int MaxAffordableScribeCount(int tierIndex)
        {
            var def = Scribes.GetDefinition(tierIndex);
            int owned = Scribes.GetOwned(tierIndex);
            double remaining = Wallet.Balance;
            int count = 0;
            while (true)
            {
                double cost = def.baseCost * Math.Pow(def.costGrowthRate, owned + count);
                if (cost > remaining) break;
                remaining -= cost;
                count++;
            }
            return count;
        }

        /// <summary>Total cost to buy ScribeBuyMultiplier more of a tier, accounting for the cost
        /// curve rising with each one bought (mirrors VerseBulkCost's shape). MaxBuyMultiplier
        /// resolves to however many the current wallet balance can actually afford.</summary>
        public double ScribeBulkCost(int tierIndex)
        {
            if (Scribes == null || !Scribes.IsUnlocked(tierIndex, NextVerseIndex)) return 0;
            var def = Scribes.GetDefinition(tierIndex);
            int owned = Scribes.GetOwned(tierIndex);
            int count = ScribeBuyMultiplier == MaxBuyMultiplier ? MaxAffordableScribeCount(tierIndex) : ScribeBuyMultiplier;
            double total = 0;
            for (int i = 0; i < count; i++)
                total += def.baseCost * Math.Pow(def.costGrowthRate, owned + i);
            return total;
        }

        /// <summary>Buys up to ScribeBuyMultiplier of a tier in one action (or as many as
        /// affordable, for the Max multiplier), stopping early if unaffordable. Returns how many
        /// were actually bought.</summary>
        public int BuyScribeBulk(int tierIndex)
        {
            if (Scribes == null || !Scribes.IsUnlocked(tierIndex, NextVerseIndex)) return 0;

            int target = ScribeBuyMultiplier == MaxBuyMultiplier ? MaxAffordableScribeCount(tierIndex) : ScribeBuyMultiplier;
            int bought = 0;
            for (int i = 0; i < target; i++)
            {
                double cost = Scribes.GetNextCost(tierIndex);
                if (!Wallet.TrySpend(cost)) break;
                Scribes.Buy(tierIndex);
                bought++;
            }
            if (bought > 0)
            {
                if (xpConfig != null && Prestige.ResetPrestigeCount > 0)
                    Levels.AddXp(xpConfig.XpPerScribePurchaseAfterReset * bought);
                OnStateChanged?.Invoke();
            }
            return bought;
        }

        public bool BookComplete => Verses == null || !Verses.HasVerse(NextVerseIndex);

        /// <summary>Grace skill tree "Swift Unlock" branch discount, clamped so verse/chapter cost
        /// can never drop below 20% of its base value - keeps the core loop meaningful even at
        /// max investment in this branch.</summary>
        private double VersePricingMultiplier => 1.0 - Math.Min(0.8, Skills.GetTotalEffect(SkillEffectType.PricingDiscount));

        public double NextVerseCost => pricingConfig != null
            ? PricingCurve.VerseCost(pricingConfig, NextVerseIndex) * VersePricingMultiplier
            : 0;

        /// <summary>Cost to unlock the verse at an arbitrary index - used by the verse list to show
        /// upcoming locked verses' costs ahead of time, not just the very next one.</summary>
        public double VerseCostAt(int index) => pricingConfig != null
            ? PricingCurve.VerseCost(pricingConfig, index) * VersePricingMultiplier
            : 0;

        /// <summary>First verse index belonging to the given (arbitrary, not necessarily current)
        /// chapter number - lets the Chapters tab jump the Verses tab to review a PAST completed
        /// chapter's verses, not just the live current chapter (2026-08-04, real gap: chapter rows
        /// were entirely unclickable before this).</summary>
        public int GetChapterStartIndex(int chapterNumber)
        {
            if (Verses == null) return -1;
            for (int i = 0; i < Verses.VerseCount; i++)
                if (Verses.GetVerse(i).ChapterNumber == chapterNumber) return i;
            return -1;
        }

        /// <summary>How many verses the given chapter number has, total (not "remaining").</summary>
        public int GetChapterVerseCount(int chapterNumber)
        {
            if (Verses == null) return 0;
            int count = 0;
            for (int i = 0; i < Verses.VerseCount; i++)
                if (Verses.GetVerse(i).ChapterNumber == chapterNumber) count++;
            return count;
        }

        public void TapForInk()
        {
            Wallet.Add(EffectiveTapAmount);
            // Tapping grants 0 XP normally - only starts granting XP once the player has
            // performed at least one Reset-Prestige (2026-08-06, user's explicit ask).
            if (xpConfig != null && Prestige.ResetPrestigeCount > 0)
                Levels.AddXp(xpConfig.XpPerTapAfterReset);
            OnStateChanged?.Invoke();
        }

        private bool TryBuyOneClickPowerNoNotify()
        {
            if (!Wallet.TrySpend(ClickPowerCost)) return false;
            ClickPowerLevel++;
            if (xpConfig != null) Levels.AddXp(xpConfig.XpPerClickPowerUpgrade);
            return true;
        }

        /// <summary>Attempts to buy the next Click Power level. Returns false if unaffordable.</summary>
        public bool BuyClickPower()
        {
            bool bought = TryBuyOneClickPowerNoNotify();
            if (bought) OnStateChanged?.Invoke();
            return bought;
        }

        /// <summary>Buys up to ClickPowerBuyMultiplier Click Power upgrades in one action, stopping
        /// early if unaffordable. Returns how many were actually bought.</summary>
        public int BuyClickPowerBulk()
        {
            int target = ClickPowerBuyMultiplier == MaxBuyMultiplier ? MaxAffordableClickPowerCount() : ClickPowerBuyMultiplier;
            int bought = 0;
            for (int i = 0; i < target; i++)
            {
                if (!TryBuyOneClickPowerNoNotify()) break;
                bought++;
            }
            if (bought > 0) OnStateChanged?.Invoke();
            return bought;
        }

        /// <summary>Shared passive multiplier applied to every owned scribe's output (2026-08-04,
        /// explicit user design) - grows with book progress independent of the per-tier owned-count
        /// milestone curve, so content progress itself keeps feeding the passive economy. +0.1 for
        /// every 5 verses purchased; doubles outright on every chapter completed. Stacks
        /// multiplicatively with the milestone curve and manager bonus in
        /// ScribeSystem.GetTierInkPerSecond.</summary>
        public float ProgressMultiplier { get; private set; } = 1f;

        /// <summary>Flat Ink/sec granted per Reset-Prestige performed, permanently - "so they're
        /// not starting with nothing and having to go click for no reason" (2026-08-06, user's
        /// explicit ask). Never resets, including on a later reset.</summary>
        private const double ResetBaseInkPerSecondPerReset = 0.5;

        /// <summary>Actual total Ink/sec, including every Grace skill tree bonus - the single
        /// source of truth both Update()'s real income tick and the UI's displayed rate read from,
        /// so the number shown always matches what's actually being earned. Skill bonuses (Ink
        /// Flow / Illuminated Pages / Scribe's Diligence branches) stack additively with each
        /// other, then apply multiplicatively on top of the milestone/manager/progress multipliers
        /// already baked into TotalInkPerSecond. This supersedes the old lean-v1 "+1% Ink per Grace
        /// ever spent" auto-bonus (PrestigeSystem.IncomeMultiplier), an explicit placeholder "until
        /// a real Grace Shop is proven out" - this tree is that shop now. Also folds in two
        /// permanent Reset-Prestige bonuses (2026-08-06): a flat base Ink/sec that stacks with
        /// every reset performed, and a book-completion multiplier unlocked once at least one book
        /// has been finished AND at least one reset has been performed, growing +1x per additional
        /// reset - "a permanent two x multiplier on however much ink they're earning."</summary>
        public double EffectiveInkPerSecond
        {
            get
            {
                if (scribeSystemsByBook.Count == 0) return 0;
                double skillIncomeBoost = 1.0
                    + Skills.GetTotalEffect(SkillEffectType.IncomeMultiplier)
                    + Skills.GetTotalEffect(SkillEffectType.ProgressMultiplierBoost)
                    + Skills.GetTotalEffect(SkillEffectType.ScribeMilestoneBoost);
                double managerBonusBoost = Skills.GetTotalEffect(SkillEffectType.ManagerBonusBoost);
                // Sums every registered book's roster (2026-08-08, multi-book economy), not just
                // the active one - a book's scribes keep earning Ink in the background after
                // switching away, explicit user decision.
                double scribeIncome = scribeSystemsByBook.Values
                    .Sum(s => s.TotalInkPerSecond(Levels.CurrentLevel, ProgressMultiplier, managerBonusBoost)) * skillIncomeBoost;

                double resetBaseBonus = Prestige.ResetPrestigeCount * ResetBaseInkPerSecondPerReset;
                double bookCompletionMultiplier = (BooksCompletedCount > 0 && Prestige.ResetPrestigeCount > 0)
                    ? 1 + Prestige.ResetPrestigeCount
                    : 1.0;

                return (scribeIncome + resetBaseBonus) * bookCompletionMultiplier;
            }
        }

        /// <summary>Advances NextVerseIndex and awards XP for the verse at the current
        /// NextVerseIndex, WITHOUT charging Ink - the caller is responsible for having already
        /// paid (either the per-verse cost, or a discounted lump sum for a chapter bulk-buy).</summary>
        private void ApplyVersePurchaseNoCharge()
        {
            var activeBook = ActiveBook;
            var purchasedVerse = Verses.GetVerse(NextVerseIndex);
            activeBook.NextVerseIndex++;

            if (NextVerseIndex % 5 == 0) ProgressMultiplier += 0.1f;

            bool chapterComplete = !Verses.HasVerse(NextVerseIndex) ||
                Verses.GetVerse(NextVerseIndex).ChapterNumber != purchasedVerse.ChapterNumber;
            if (chapterComplete)
            {
                activeBook.ChaptersCompletedInBook++;
                ProgressMultiplier *= 2f;
            }
            // BooksCompletedCount is now computed from BookProgress.IsComplete - no increment
            // needed here, it just becomes true once NextVerseIndex runs past the book's last verse.

            if (xpConfig != null)
            {
                // Reset-Prestige permanently raises verse/chapter/book XP (2026-08-06) - once the
                // player has ever performed an opt-in reset, every subsequent verse purchase uses
                // the bigger values, regardless of how many more resets happen after.
                bool afterReset = Prestige.ResetPrestigeCount > 0;
                Levels.AddXp(afterReset ? xpConfig.XpPerVerseAfterReset : xpConfig.XpPerVerse);
                if (chapterComplete) Levels.AddXp(afterReset ? xpConfig.XpPerChapterBonusAfterReset : xpConfig.XpPerChapterBonus);
                if (BookComplete) Levels.AddXp(afterReset ? xpConfig.XpPerBookBonusAfterReset : xpConfig.XpPerBookBonus);
            }
        }

        private bool TryBuyOneVerseNoNotify()
        {
            if (BookComplete || pricingConfig == null) return false;
            if (RequiresChapterUnlock) return false; // must use BuyNextChapter first - see RequiresChapterUnlock

            if (!Wallet.TrySpend(NextVerseCost)) return false;

            ApplyVersePurchaseNoCharge();
            return true;
        }

        /// <summary>Grace reward for prestiging right now, before the opt-in reset's 2.5x
        /// multiplier. Uses Wallet.LifetimeEarned (never decreases on spend), not the current
        /// spendable Balance - per the confirmed Grace formula.</summary>
        public double PrestigeGracePreview =>
            PrestigeSystem.CalculateGraceReward(Wallet.LifetimeEarned, NextVerseIndex, ChaptersCompletedCount, BooksCompletedCount)
            * (1.0 + Skills.GetTotalEffect(SkillEffectType.GraceGainBonus));

        /// <summary>Grace reward including the opt-in reset path's 2.5x multiplier - shown as the
        /// "with reset" preview alongside the plain PrestigeGracePreview.</summary>
        public double PrestigeGracePreviewWithReset => PrestigeGracePreview * 2.5;

        /// <summary>
        /// Performs a prestige cycle. The free path only awards Grace - Level/XP, Ink, Click
        /// Power, and scribe owned counts are all left untouched (2026-08-05, explicit user
        /// correction: the free path must never reset the XP bar). The opt-in reset path also
        /// resets Level/XP back to 1/0, wipes Ink balance, Click Power level, and every scribe
        /// tier's owned count, for 2.5x the Grace. Verses, chapters, and books already unlocked are
        /// NEVER reset on either path - the one hard rule that doesn't bend, since it's what
        /// protects the memorization mission. Returns false if not yet eligible.
        /// </summary>
        public bool PerformPrestige(bool withReset)
        {
            if (!Levels.IsPrestigeEligible) return false;

            double grace = withReset ? PrestigeGracePreviewWithReset : PrestigeGracePreview;
            Prestige.AwardGrace(grace, withReset);

            if (withReset)
            {
                Levels.ResetForPrestige();
                Wallet.ResetBalance();
                ClickPowerLevel = 0;
                // Every registered book's scribe owned-counts reset (2026-08-08), not just the
                // active book's - a reset wipes the whole shared economy, matching how Ink/Click
                // Power/Level reset globally too.
                foreach (var sys in scribeSystemsByBook.Values) sys.ResetOwned();
            }

            OnStateChanged?.Invoke();
            return true;
        }

        /// <summary>True when NextVerseIndex sits at the very first verse of a chapter beyond the
        /// book's first one, nothing has been bought in it yet, AND the player hasn't explicitly
        /// unlocked it via UnlockCurrentChapter() - meaning individual verse purchases (single or
        /// bulk) are blocked until either UnlockCurrentChapter() (free - just opens the gate) or
        /// BuyNextChapter() (paid - opens the gate AND buys every verse in it at once) is used.
        /// 2026-08-04, explicit user design: verse purchases were silently crossing chapter
        /// boundaries mid-bulk-buy, which was never intended. 2026-08-05, real bug fix: the ONLY
        /// unlock mechanism was BuyNextChapter, which bought every verse in the chapter at once -
        /// defeating the entire point of a per-verse Verses tab. UnlockCurrentChapter() is the
        /// missing "just let me pick verses individually" action. The gate itself lives on the
        /// active book's own BookProgress record (2026-08-06) - -1 matches no real chapter, so the
        /// very first gated boundary always requires an explicit unlock, and each book tracks its
        /// own gate independently once book-switching exists.</summary>
        public bool RequiresChapterUnlock =>
            !BookComplete && NextVerseIndex > 0 && NextVerseIndex == CurrentChapterStartIndex
            && ActiveBook?.UnlockedChapterNumber != CurrentChapterNumber;

        /// <summary>Opens the current chapter's gate for free, WITHOUT buying any verse - lets the
        /// player then buy verses one at a time (or in a VerseBuyMultiplier bulk, still capped at
        /// this chapter's boundary) from the Verses tab. Returns false if the gate isn't currently
        /// blocking anything.</summary>
        public bool UnlockCurrentChapter()
        {
            if (!RequiresChapterUnlock) return false;
            ActiveBook.UnlockedChapterNumber = CurrentChapterNumber;
            OnStateChanged?.Invoke();
            return true;
        }

        /// <summary>Chapter number the player is currently working through (containing
        /// NextVerseIndex) - every earlier chapter is fully bought, every later one untouched,
        /// since verses are always bought in strict canonical order.</summary>
        public int CurrentChapterNumber => !BookComplete ? Verses.GetVerse(NextVerseIndex).ChapterNumber : -1;

        /// <summary>How many verses remain unbought in the current chapter.</summary>
        public int RemainingVersesInCurrentChapter
        {
            get
            {
                if (BookComplete) return 0;
                int chapter = CurrentChapterNumber;
                int count = 0;
                for (int i = NextVerseIndex; Verses.HasVerse(i) && Verses.GetVerse(i).ChapterNumber == chapter; i++)
                    count++;
                return count;
            }
        }

        /// <summary>First verse index belonging to the current chapter (scans backward from
        /// NextVerseIndex, bounded by the chapter itself, not the whole book).</summary>
        public int CurrentChapterStartIndex
        {
            get
            {
                if (BookComplete) return -1;
                int chapter = CurrentChapterNumber;
                int i = NextVerseIndex;
                while (i > 0 && Verses.GetVerse(i - 1).ChapterNumber == chapter) i--;
                return i;
            }
        }

        /// <summary>One past the last verse index belonging to the current chapter.</summary>
        public int CurrentChapterEndIndexExclusive => BookComplete ? -1 : NextVerseIndex + RemainingVersesInCurrentChapter;

        /// <summary>Cost to buy every remaining verse in the current chapter at once, per the
        /// documented chapter bulk-buy discount: sum of individual verse costs x 0.75.</summary>
        public double ChapterBulkCost
        {
            get
            {
                if (BookComplete) return 0;
                int chapter = CurrentChapterNumber;
                double sum = 0;
                for (int i = NextVerseIndex; Verses.HasVerse(i) && Verses.GetVerse(i).ChapterNumber == chapter; i++)
                    sum += VerseCostAt(i);
                return sum * 0.75;
            }
        }

        /// <summary>Buys every remaining verse in the current chapter in one action, at the
        /// discounted ChapterBulkCost (paid as a single lump sum, not per-verse). Returns how many
        /// verses were actually bought (0 if unaffordable).</summary>
        public int BuyNextChapter()
        {
            if (BookComplete) return 0;
            int remaining = RemainingVersesInCurrentChapter;
            if (remaining <= 0) return 0;

            double cost = ChapterBulkCost;
            if (!Wallet.TrySpend(cost)) return 0;

            for (int i = 0; i < remaining; i++)
                ApplyVersePurchaseNoCharge();

            if (remaining >= 5 && xpConfig != null) Levels.AddXp(xpConfig.XpBulkBuyBonus);
            OnStateChanged?.Invoke();
            return remaining;
        }

        /// <summary>Attempts to buy the next verse in canonical order. Returns false if unaffordable or book is complete.</summary>
        public bool BuyNextVerse()
        {
            bool bought = TryBuyOneVerseNoNotify();
            if (bought) OnStateChanged?.Invoke();
            return bought;
        }

        /// <summary>Buys up to VerseBuyMultiplier verses in one action, stopping early if
        /// unaffordable or the book completes. Buying 5+ in one action awards a one-time bulk XP
        /// bonus on top of each verse's own per-verse/chapter/book XP. Returns how many were
        /// actually bought.</summary>
        public int BuyVersesBulk()
        {
            int target = VerseBuyMultiplier == MaxBuyMultiplier ? MaxAffordableVerseCount() : VerseBuyMultiplier;
            int bought = 0;
            for (int i = 0; i < target; i++)
            {
                if (!TryBuyOneVerseNoNotify()) break;
                bought++;
            }
            if (bought >= 5 && xpConfig != null) Levels.AddXp(xpConfig.XpBulkBuyBonus);
            if (bought > 0) OnStateChanged?.Invoke();
            return bought;
        }

        /// <summary>Attempts to buy the next rank of a Grace skill tree node. Returns false if the
        /// node is unknown, maxed, locked by its prerequisite, or unaffordable.</summary>
        public bool BuySkill(string nodeId)
        {
            var node = prestigeSkillTreeConfig?.FindNode(nodeId);
            if (node == null || !Skills.CanBuy(node, Prestige.Grace, Prestige.ResetPrestigeCount > 0)) return false;

            double cost = Skills.Buy(node);
            Prestige.TrySpendGrace(cost);
            OnStateChanged?.Invoke();
            return true;
        }

        public bool HasUnlockedVerse => NextVerseIndex > 0;

        public VerseDatabase.FlatVerse LastUnlockedVerse => Verses.GetVerse(NextVerseIndex - 1);
    }
}
