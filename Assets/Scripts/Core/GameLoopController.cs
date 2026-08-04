using System;
using ClickerGenesis.Data;
using ClickerGenesis.Economy;
using ClickerGenesis.Progression;
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
        [Header("Config")]
        [SerializeField] private VersePricingConfig pricingConfig;
        [SerializeField] private XpConfig xpConfig;
        [SerializeField] private ScribeSetConfig scribeConfig;
        [SerializeField] private string verseResourcePath = "Verses/genesis_1";
        [SerializeField] private double tapAmount = 1;
        [SerializeField] private double startingInk = 0;

        [Header("Click Power upgrade")]
        [SerializeField] private double clickPowerBaseCost = 25;
        [SerializeField] private double clickPowerCostGrowthRate = 1.15;

        public InkWallet Wallet { get; private set; }
        public VerseDatabase Verses { get; private set; }
        public LevelSystem Levels { get; private set; }
        public ScribeSystem Scribes { get; private set; }

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
            tapAmount * (1.0 + ClickPowerPerLevelGrowth * ClickPowerLevel) * MilestoneCurve.GetMultiplier(ClickPowerLevel);

        /// <summary>What EffectiveTapAmount becomes after the next Click Power purchase — used to
        /// preview the upgrade's payoff on its button label instead of showing only its cost.</summary>
        public double NextEffectiveTapAmount =>
            tapAmount * (1.0 + ClickPowerPerLevelGrowth * (ClickPowerLevel + 1)) * MilestoneCurve.GetMultiplier(ClickPowerLevel + 1);

        /// <summary>Index (within the book) of the next verse that has not yet been purchased.</summary>
        public int NextVerseIndex { get; private set; }

        [Header("Bulk buy")]
        private static readonly int[] VerseMultiplierTiers = { 1, 5, 10, 20 };
        private static readonly int[] ClickPowerMultiplierTiers = { 1, 5, 10, 20, 100 };

        /// <summary>How many verses "Buy Next Verse" purchases per click. Cycles 1/5/10/20 - 20 is
        /// the max, per explicit request ("I'm not going higher than that").</summary>
        public int VerseBuyMultiplier { get; private set; } = 1;

        /// <summary>How many Click Power upgrades "Upgrade Tap" buys per click. Cycles
        /// 1/5/10/20/100 - the button to change this only appears once ClickPowerLevel >= 5.</summary>
        public int ClickPowerBuyMultiplier { get; private set; } = 1;

        public void CycleVerseBuyMultiplier()
        {
            int idx = (System.Array.IndexOf(VerseMultiplierTiers, VerseBuyMultiplier) + 1) % VerseMultiplierTiers.Length;
            VerseBuyMultiplier = VerseMultiplierTiers[idx];
            OnStateChanged?.Invoke();
        }

        public void CycleClickPowerBuyMultiplier()
        {
            int idx = (System.Array.IndexOf(ClickPowerMultiplierTiers, ClickPowerBuyMultiplier) + 1) % ClickPowerMultiplierTiers.Length;
            ClickPowerBuyMultiplier = ClickPowerMultiplierTiers[idx];
            OnStateChanged?.Invoke();
        }

        /// <summary>Total cost to buy VerseBuyMultiplier verses starting from NextVerseIndex,
        /// capped at however many verses remain in the book.</summary>
        public double VerseBulkCost
        {
            get
            {
                double total = 0;
                for (int i = 0; i < VerseBuyMultiplier && Verses != null && Verses.HasVerse(NextVerseIndex + i); i++)
                    total += VerseCostAt(NextVerseIndex + i);
                return total;
            }
        }

        /// <summary>Total cost to buy ClickPowerBuyMultiplier Click Power upgrades in a row.</summary>
        public double ClickPowerBulkCost
        {
            get
            {
                double total = 0;
                for (int i = 0; i < ClickPowerBuyMultiplier; i++)
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
                return tapAmount * (1.0 + ClickPowerPerLevelGrowth * afterLevel) * MilestoneCurve.GetMultiplier(afterLevel);
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

            Wallet = new InkWallet(startingInk);
            Verses = VerseDatabase.LoadFromResources(verseResourcePath);
            Levels = new LevelSystem(xpConfig);
            if (scribeConfig != null) Scribes = new ScribeSystem(scribeConfig);
            NextVerseIndex = 0;

            if (Application.isPlaying) GameSettings.ApplyDisplaySettings();
        }

        private void Update()
        {
            if (Scribes == null) return;

            double inkPerSecond = Scribes.TotalInkPerSecond(Levels.CurrentLevel);
            if (inkPerSecond <= 0) return;

            Wallet.Add(inkPerSecond * Time.deltaTime);
            OnStateChanged?.Invoke();
        }

        /// <summary>Attempts to buy one more of a scribe tier. Returns false if unaffordable, locked, or config missing.</summary>
        public bool BuyScribe(int tierIndex)
        {
            if (Scribes == null) return false;
            if (!Scribes.IsUnlocked(tierIndex, NextVerseIndex)) return false;

            double cost = Scribes.GetNextCost(tierIndex);
            if (!Wallet.TrySpend(cost)) return false;

            Scribes.Buy(tierIndex);
            OnStateChanged?.Invoke();
            return true;
        }

        public bool BookComplete => Verses == null || !Verses.HasVerse(NextVerseIndex);

        public double NextVerseCost => pricingConfig != null
            ? PricingCurve.VerseCost(pricingConfig, NextVerseIndex)
            : 0;

        /// <summary>Cost to unlock the verse at an arbitrary index - used by the verse list to show
        /// upcoming locked verses' costs ahead of time, not just the very next one.</summary>
        public double VerseCostAt(int index) => pricingConfig != null ? PricingCurve.VerseCost(pricingConfig, index) : 0;

        public void TapForInk()
        {
            Wallet.Add(EffectiveTapAmount);
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
            int bought = 0;
            for (int i = 0; i < ClickPowerBuyMultiplier; i++)
            {
                if (!TryBuyOneClickPowerNoNotify()) break;
                bought++;
            }
            if (bought > 0) OnStateChanged?.Invoke();
            return bought;
        }

        /// <summary>Advances NextVerseIndex and awards XP for the verse at the current
        /// NextVerseIndex, WITHOUT charging Ink - the caller is responsible for having already
        /// paid (either the per-verse cost, or a discounted lump sum for a chapter bulk-buy).</summary>
        private void ApplyVersePurchaseNoCharge()
        {
            var purchasedVerse = Verses.GetVerse(NextVerseIndex);
            NextVerseIndex++;

            if (xpConfig != null)
            {
                Levels.AddXp(xpConfig.XpPerVerse);

                bool chapterComplete = !Verses.HasVerse(NextVerseIndex) ||
                    Verses.GetVerse(NextVerseIndex).ChapterNumber != purchasedVerse.ChapterNumber;
                if (chapterComplete) Levels.AddXp(xpConfig.XpPerChapterBonus);

                if (BookComplete) Levels.AddXp(xpConfig.XpPerBookBonus);
            }
        }

        private bool TryBuyOneVerseNoNotify()
        {
            if (BookComplete || pricingConfig == null) return false;
            if (!Wallet.TrySpend(NextVerseCost)) return false;

            ApplyVersePurchaseNoCharge();
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
            int bought = 0;
            for (int i = 0; i < VerseBuyMultiplier; i++)
            {
                if (!TryBuyOneVerseNoNotify()) break;
                bought++;
            }
            if (bought >= 5 && xpConfig != null) Levels.AddXp(xpConfig.XpBulkBuyBonus);
            if (bought > 0) OnStateChanged?.Invoke();
            return bought;
        }

        public bool HasUnlockedVerse => NextVerseIndex > 0;

        public VerseDatabase.FlatVerse LastUnlockedVerse => Verses.GetVerse(NextVerseIndex - 1);
    }
}
