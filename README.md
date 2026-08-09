# Clicker Genesis

A Bible-verse idle/incremental clicker built in Unity, for Windows, macOS, and Android. Tap to earn Ink, spend Ink to unlock the next verse of Scripture in canonical order, and grow a roster of themed "scribes" that keep generating Ink while you're away — all wrapped around the goal of helping players actually **read and memorize the Bible** through play.

> Micro-transactions exist in the eventual design but are never required for progress or content. Scripture text is never altered, paraphrased, or reinterpreted — only unedited, recognized translations are used (KJV in v1). This constraint overrides convenience in every implementation decision.

<p align="center">
  <img src="docs/screenshots/clicker-scribes.png" width="45%" alt="Clicker screen, Scribes tab — tap for Ink, passive scribe income, milestone bonuses" />
  <img src="docs/screenshots/clicker-managers.png" width="45%" alt="Clicker screen, Managers tab — color-coded unlock requirements, real bonus display" />
</p>
<p align="center">
  <img src="docs/screenshots/clicker-support.png" width="45%" alt="Clicker screen, Support tab — hire submanagers for cost discounts or output boosts" />
  <img src="docs/screenshots/buy-verse-screen.png" width="45%" alt="Buy Verse screen — spend Ink on the next verse, real KJV text reveal" />
</p>
<p align="center">
  <img src="docs/screenshots/skill-tree.png" width="70%" alt="Grace Skill Tree — radial node layout against a nebula backdrop" />
</p>

## Status

**Version 0.4.0** — pre-release, core loop implemented and playable. Headline change: a full **Achievement system** — 655 real achievements (headline milestones, per-book completion, per-manager unlocks, per-manager "household complete" submanager-group achievements, and section-groupings like "The Five Books of Moses") generated straight from the game's own live Scribes/Managers data, browsable in a tabbed card-grid Achievements screen with bronze/silver/gold difficulty tiers and spoiler-safe hidden achievements. Also in this build: real mobile-device layout fixes are underway after testing on a physical Android phone surfaced genuine portrait-layout bugs (list-row text overlap, wrong Canvas Scaler reference resolution) — the worst of these are fixed, a device-responsive orientation system is in progress, and a manual portrait/landscape Settings toggle is still to come. No new release build has been cut for 0.4.0 yet — this is a source commit checkpoint ahead of the next build pass; the last published build is still v0.3.3 below.

This is an active work-in-progress being built in public as part of a development portfolio — see [Bug Tracker](#bug-tracker--known-issues) for a transparent, running log of what's broken and what's been fixed.

## Download

Three platforms — pick yours (still on v0.3.3, the last cut release; v0.3.4 is source-only until mobile bugs are triaged):

| Platform | Download | Minimum version |
|---|---|---|
| 🪟 Windows | [ClickerGenesis-Windows-v0.3.3.zip](https://github.com/neogentrics/Clicker-Genesis/releases/download/v0.3.3/ClickerGenesis-Windows-v0.3.3.zip) | Windows 10 64-bit or later |
| 🍎 macOS | [ClickerGenesis-Mac-v0.3.3.zip](https://github.com/neogentrics/Clicker-Genesis/releases/download/v0.3.3/ClickerGenesis-Mac-v0.3.3.zip) | macOS 12.0 (Monterey) or later |
| 🤖 Android | [ClickerGenesis-Android-v0.3.3.apk](https://github.com/neogentrics/Clicker-Genesis/releases/download/v0.3.3/ClickerGenesis-Android-v0.3.3.apk) | Android 8.0 (Oreo, API 26) or later |

- **Windows:** unzip and run `ClickerGenesis.exe`.
- **macOS:** unzip, then right-click `ClickerGenesis.app` → **Open** the first time (the build is unsigned/unnotarized — no Apple Developer account in this pipeline yet — so Gatekeeper blocks a plain double-click on first launch; this is expected for an indie unsigned build, not broken packaging).
- **Android:** download the `.apk` directly to your device and install it (you'll need to allow installs from this source in Android settings — this is a test build, not distributed via the Play Store). This is the first mobile test pass, so expect rough edges on touch input and small-screen layout.

All releases (including older versions and full changelogs) are on the [Releases page](https://github.com/neogentrics/Clicker-Genesis/releases).

## Core loop

1. **Tap** on the Clicker screen to earn Ink (the game's currency).
2. **Buy scribes** — passive Ink/sec generators, themed per book (Genesis: Reed Pen → Papyrus Scroll → Stone Tablet → Oil Lamp → Ark's Manifest → Covenant Seal → Jacob's Ladder → Joseph's Storehouse), each unlocking further as you progress through the book's verses.
3. **Switch to the Buy Verse screen** and spend Ink to unlock the *next* verse in canonical order — reading it reveals the verse text and reference. Verses can also be bought in bulk per chapter at a 25% discount.
4. **Level up** from XP earned on verse/chapter/book completion. Crossing a level threshold makes prestige available.
5. **Prestige** via the Grace Skill Tree — a free path (keeps everything, grants Grace only) or an opt-in reset (wipes Ink/Click Power/scribes/level for 2.5x the Grace, plus permanent post-reset bonuses). Grace buys permanent upgrades across 8 economy branches and a Book Progression branch that unlocks the next book — nothing already-unlocked (verses, ranks) is ever re-locked by either path.
6. **Switch books** once unlocked (Books tab, appears after your first prestige) — each book keeps its own independent progress, so switching to a newly-unlocked book never touches what you've already read in an earlier one.

## Features implemented so far

- Tap-to-earn Ink with a purchasable Click Power upgrade (smooth per-purchase growth + milestone multiplier breakpoints at 10/25/50/100 owned, shared with scribe scaling)
- **19-tier Genesis scribe roster** (Reed Pen through Silver Cup) with named managers (Adam through Benjamin) gated on their real first-mention verse in scripture, plus a working **submanager system** — minor characters (Seth, Methuselah, Hagar, Melchizedek, and 15 others) hireable under a manager for a scribe-cost discount or an output-bonus boost, each with their own real verse gate. An auto-buy toggle with a spendable-Ink reserve floor covers the whole scribe list.
- Full Genesis KJV text (50 chapters, 1,533 verses) plus all 38 remaining Old Testament books staged and switchable, sequential verse unlocking with a scrollable, referenced ("Genesis 1:1") verse list
- Chapter bulk-buy ("Buy Next Chapter") with a documented 25% discount vs. buying verses individually, split into a free "Unlock Chapter" gate + a paid "Complete Chapter" bulk-buy
- Bulk-buy multipliers (1x/5x/10x/20x, and up to 100x for Click Power) on both verse and click-power purchases
- XP/leveling system driven entirely by content-completion (verse/chapter/book), not combat — feeding prestige eligibility
- **Prestige system**: a 105-node Grace Skill Tree (radial layout, 8 economy branches + a Book Progression outer ring) reached via a dedicated Skill Tree screen, gated behind a central "Core" hub node. Free prestige (Grace only, nothing lost) vs. opt-in Reset-Prestige (2.5x Grace, wipes numeric progress only, never unlocked verses/skill ranks) — resetting grants permanent stacking Ink/sec and book-completion-multiplier bonuses, plus raised post-reset XP rates
- **Books tab**: switch your active book once unlocked via the Grace tree, enforcing "finish book N before starting book N+1" — each book tracks its own independent verse/chapter progress
- **Stats screen** (Pause Menu): lifetime Ink earned/spent, Grace earned/spent, prestige counts, skills/managers bought, verses/chapters/books completed
- Multi-scene navigation (Main Menu → Clicker Screen ↔ Buy Verse Screen ↔ Skill Tree, Settings) with persistent cross-scene game state
- Working sound toggle (PlayerPrefs-persisted)
- Parchment/ink/gold "warm, legible, mobile-fast" base art direction, with a reserved stained-glass accent style for reward moments (not yet built)
- Every screen re-themed with a shared stone-textured backdrop instead of flat color, giving the UI real depth
- Managers auto-purchase their own scribe tier once bought, on top of their existing output bonus, and their tab row now shows only their name and actual active bonus (no flavor text)
- **Scribes / Managers / Support** three-tab layout: Managers show every unmet unlock requirement (scribe tier, character's own verse, level) as its own color-coded line — green when satisfied, red when not — with the buy button showing only the real cost, never lock-reason text. Submanagers moved into their own dedicated Support tab, same requirement-line pattern, decluttering the Managers list
- Run-in-background toggle (Settings) so idle Ink income keeps ticking while the window is unfocused
- Shared passive "progress multiplier": every 5 verses bought adds +0.1 to a multiplier applied across all scribe output, and it doubles outright on every chapter completed
- Real hover/press visual feedback on every button in the game, plus tooltips throughout
- Managers are correctly locked until their own scribe tier is unlocked by verse progress, not just by player level — and that gating stays keyed to Genesis' progress specifically, so switching to a different active book never re-locks already-unlocked scribes/managers
- **Full Old Testament, all 39 books playable**: every book from Genesis through Malachi has its own real scribe/manager/submanager roster (408 tiers total), computed against its real KJV verse positions, switchable via the Books tab in canonical order
- **All 27 New Testament books' data imported** (276 more tiers — Matthew through Revelation, each Gospel's Jesus independently authored per book) but intentionally not switchable in-game yet — a "New Testament — Coming Soon" row shows in the Books tab instead, until the Old Testament experience is polished
- **Save/load persistence**: local JSON save file (not in-memory-only), atomic temp-file-then-rename writes with a `.bak` fallback, light obfuscation, and graceful corruption recovery (falls back primary → backup → fresh save on any read failure) — verified via a real save → exit → reload round trip and both corruption-recovery paths
- **Achievement system (new in 0.4.0)**: 655 real achievements generated from the game's own live data, not hand-typed — 25 hand-authored headline milestones, 66 per-book completion achievements (39 OT + 27 NT), 10 section-grouping/"ultimate" achievements (e.g. completing the Pentateuch, the Four Gospels, the full Bible), 386 per-manager unlock achievements, and 168 per-manager "household complete" achievements (hire every submanager under a given manager). Browsable via a Bloons-TD6-style tabbed card grid (category tabs + live search) with bronze/silver/gold difficulty-tier diamond frames and real category icon art. Spoiler-hidden achievements show "Hidden Achievement" until earned so discovering a new named character stays a surprise. Global persistence, independent of save slots, so progress is visible no matter which slot is active.

## Tech stack

- **Engine:** Unity 6.5, URP 3D Core template
- **Language:** C#, ScriptableObject-driven config (pricing curves, XP constants, scribe rosters — nothing gameplay-numeric is hardcoded, so it can be rebalanced after playtesting without touching code)
- **UI:** TextMeshPro, Ibarra Real Nova (openly licensed, replacing an earlier Georgia font asset that was flagged as a licensing risk for a shipped build)
- **Tooling:** [Unity-MCP](https://github.com/IvanMurzak/Unity-MCP) for AI-assisted Editor/scene/Play-mode workflows during development
- **Source control:** Git + Git LFS

## Project structure

```
Assets/
  Scripts/
    Core/          GameLoopController (persistent singleton), screen UI controllers, scene nav
    Economy/        Ink wallet, scribe system, milestone curve
    Progression/     XP/leveling, Prestige/Grace Skill Tree system
    Data/           Verse database, pricing config, per-book progress, canonical book order
  Config/           ScriptableObject assets (XpConfig, GenesisScribes, pricing curves, Grace Skill Tree)
  Resources/
    Bible/           Canonical KJV outline (all 66 books, chapter/verse counts)
    Verses/          Loaded verse text by book (all 39 Old Testament books staged)
  Scenes/           MainMenu, ClickerScreen, BuyVerseScreen, PrestigeScreen, SettingsScreen, CoreLoopTest (dev/smoke-test)
```

## Getting started

1. Clone with LFS: `git lfs install && git clone https://github.com/neogentrics/Clicker-Genesis.git`
2. Open in Unity 6.5 (URP).
3. Open `Assets/Scenes/MainMenu.unity` and press Play — this is the real production entry point (not `CoreLoopTest.unity`, which is a standalone dev/smoke-test scene for the core tap→buy loop only).

## Bug tracker / known issues

Every reported bug is tracked with an increasing number across two columns — **Open** (not yet confirmed fixed) and **Fixed** (explicitly confirmed by real playtesting, not just by the code changing). Bugs only move to Fixed once they've been verified in an actual Play session — not from a screenshot or a script check alone.

Full tracker (kept in sync, includes fix descriptions and root causes): see the project's Notion Bug Tracker page (private), or the live [GitHub Issues](../../issues) list — every bug here has a matching Issue, closed once confirmed fixed. Open a new Issue for anything new you find.

## Versioning

`X.Y.Z`:
- **X** — official release marker (`0` = pre-release)
- **Y** — feature/update bump. Only increases once the current feature set has been confirmed genuinely playable end-to-end through real testing — not just when a feature-shaped chunk of code lands.
- **Z** — patch/bugfix only

## Open questions / roadmap

- Whether Psalms should be a starter-book option or gated behind prestige (needs real per-chapter verse-count analysis — already gathered, decision deferred)
- Real narrative-accurate scribe/manager unlock thresholds beyond the current placeholder mechanism-testing values
- NIV/Amplified translations (on hold pending licensing review)
- Real audio (volume slider, SFX/BGM, verse narration) — not started
- Real mobile-device responsive layout — a physical Android device test surfaced genuine portrait-layout bugs (wrong Canvas Scaler reference resolution on several screens, list rows with real text-on-text overlap, the Skill Tree rendering as a tiny dot on a narrow screen). The worst of these are fixed and verified; still open: a true single-column phone reflow for the Clicker/Buy Verse screens (currently just the wide desktop layout scaled down), the Skill Tree's own zoom baseline (Bug #58), and a manual portrait/landscape toggle in Settings (the auto-rotate-following logic underneath it is already built)
- Also actively in progress, not yet playable: a 3-save-slot system (replacing the earlier single implicit save), and a full Geneva Bible 1560 + Ethiopian Bible data layer (231 books' worth of scribe/manager content indexed alongside KJV's — extracted, validated, not yet wired into gameplay)

## Guiding principle

Scripture text is never altered, paraphrased, or reinterpreted. Only unedited, recognized translations are used. This constraint overrides convenience in any implementation decision — it's the one rule in this project that doesn't get relaxed for the sake of shipping faster.
