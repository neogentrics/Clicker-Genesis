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

**Version 0.4.6 Beta** — pre-release, core loop implemented and playable. Recent headline changes: the game now **locks to landscape orientation** at the OS level (portrait/live-rotation was built and verified working, but Unity's `CanvasScaler` can't correctly serve one layout in both orientations at once — landscape-only was the better call, and it resolves the "wide desktop layout on a real phone" bug for good); a full **Settings screen readability/UX pass** (toggle buttons now show ON/OFF with a real glow directly on the button instead of separate inert text, cycle buttons show their actual selected value, all row text switched to bold for legibility against parchment); a real **Font Family selector** (accessibility — pick between Ibarra Real Nova, Jost, Roboto, or Liberation Sans, live-applies everywhere, no restart); and **real purchase-click SFX** wired into every Scribe/Manager/Support/Verse/Chapter buy button (was silent before). 0.4.5 shipped a real **Achievement screen visual overhaul** — shader-driven cards color-coded by category — plus a **Sound & Audio System** (mixer-driven Master/SFX/Music/Voice volume, music zones per screen), a **desktop auto-update system** (opt-in — nothing downloads without an explicit click), and **Credits/Stats** promoted to their own standalone screens. 0.4.1 shipped a redesigned **Grace Skill Tree V2** (fog-of-war constellation UI, 146 nodes of real Old Testament content). 0.4.0 shipped the **Achievement system** itself — now at **740 real achievements** generated straight from the game's own live data, browsable in a tabbed card grid with bronze/silver/gold difficulty tiers and spoiler-safe hidden entries.

Also built since 0.4.1, not yet its own version bump: a real **3-slot save system** — New Game walks you through a translation and starting-book choice, each of the 3 slots is independently save/load/delete/copy-able and shows its own completion summary (active book, level, % complete) from the slot picker.

This is an active work-in-progress being built in public as part of a development portfolio — see [Bug Tracker](#bug-tracker--known-issues) for a transparent, running log of what's broken and what's been fixed.

## Download

| Platform | Download | Minimum version |
|---|---|---|
| 🪟 Windows | [ClickerGenesis-Windows-v0.4.6.zip](https://github.com/neogentrics/Clicker-Genesis/releases/download/v0.4.6/ClickerGenesis-Windows-v0.4.6.zip) | Windows 10 64-bit or later |
| 🍎 macOS | [ClickerGenesis-Mac-v0.4.6.zip](https://github.com/neogentrics/Clicker-Genesis/releases/download/v0.4.6/ClickerGenesis-Mac-v0.4.6.zip) | macOS 12.0 (Monterey) or later |
| 🐧 Linux | [ClickerGenesis-Linux-v0.4.6.zip](https://github.com/neogentrics/Clicker-Genesis/releases/download/v0.4.6/ClickerGenesis-Linux-v0.4.6.zip) *(first Linux build for this project)* | Most 64-bit distros with a modern GL/Vulkan driver |
| 🤖 Android | [ClickerGenesis-Android-v0.4.6.apk](https://github.com/neogentrics/Clicker-Genesis/releases/download/v0.4.6/ClickerGenesis-Android-v0.4.6.apk) | Android 8.0 (Oreo, API 26) or later |

> All four platforms now ship together on every release, per the project's standing release-process rule — no more platform-lag between Windows and the rest.

- **Windows:** unzip and run `ClickerGenesis.exe`.
- **macOS:** unzip, then right-click `ClickerGenesis.app` → **Open** the first time (the build is unsigned/unnotarized — no Apple Developer account in this pipeline yet — so Gatekeeper blocks a plain double-click on first launch; this is expected for an indie unsigned build, not broken packaging).
- **Linux:** unzip, `chmod +x ClickerGenesis.x86_64`, then run it.
- **Android:** download the `.apk` directly to your device and install it (you'll need to allow installs from this source in Android settings — this is a test build, not distributed via the Play Store). Orientation is locked to landscape as of 0.4.6; expect some remaining rough edges on touch input.

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
- **Save/load persistence**: local JSON save files, atomic temp-file-then-rename writes with a `.bak` fallback, light obfuscation, and graceful corruption recovery (falls back primary → backup → fresh save on any read failure). Built out into a full **3-slot system**: New Game walks through a translation and starting-book choice, each slot is independently save/load/delete/copy-able, and the slot picker shows a real summary (active book, level, completion %) per slot without loading it.
- **Achievement system**: 740 real achievements generated from the game's own live data, not hand-typed — 25 hand-authored headline milestones, 66 per-book completion achievements (39 OT + 27 NT), 10 section-grouping/"ultimate" achievements (e.g. completing the Pentateuch, the Four Gospels, the full Bible), 386 per-manager unlock achievements, 168 per-manager "household complete" achievements (hire every submanager under a given manager), and 85 gameplay-stat ladders (lifetime Ink earned/spent, taps, and more). Browsable via a tabbed card grid (category tabs + live search) with bronze/silver/gold difficulty-tier diamond frames, real category icon art, and shader-driven card materials that recolor by category and lighten once unlocked. Spoiler-hidden achievements show "Hidden Achievement" until earned so discovering a new named character stays a surprise. Global persistence, independent of save slots, so progress is visible no matter which slot is active.
- **Sound & Audio System**: a real AudioMixer-driven Master/SFX/Music/Voice volume stack with independent mute, music that crossfades per screen zone (Menu/Core Gameplay/Skill Tree/Achievements), and SFX hooks already wired at verse/chapter/book/achievement-unlock call sites. No audio clips exist yet, so it's currently silent but fully functional — ready for content.
- **Desktop auto-update**: an opt-in "Check for Updates" / "Install" control in Settings (Velopack-based) — nothing downloads or installs without an explicit click.
- **Credits and Stats** each have their own standalone, scrollable screen (promoted from small Pause Menu popups) for real readability at scale.

## Tech stack

- **Engine:** Unity 6.5, URP 3D Core template
- **Language:** C#, ScriptableObject-driven config (pricing curves, XP constants, scribe rosters — nothing gameplay-numeric is hardcoded, so it can be rebalanced after playtesting without touching code)
- **UI:** TextMeshPro, Ibarra Real Nova (openly licensed, replacing an earlier Georgia font asset that was flagged as a licensing risk for a shipped build)
- **Tooling:** [Unity-MCP](https://github.com/IvanMurzak/Unity-MCP) for AI-assisted Editor/scene/Play-mode workflows during development
- **Auto-update:** [Velopack](https://github.com/velopack/velopack) (opt-in, desktop only)
- **Source control:** Git + Git LFS

## Project structure

```
Assets/
  _Scripts/
    Core/           GameLoopController (persistent singleton), screen UI controllers, scene nav
    Economy/        Ink wallet, scribe system, milestone curve
    Progression/    XP/leveling, Prestige/Grace Skill Tree system
    Achievements/   Achievement data model + runtime system
    Save/           Save-slot storage, migration, achievement persistence
    Data/           Verse database, pricing config, per-book progress, canonical book order
    Editor/         Editor-only tooling (Skill Tree content generation, etc.)
  _Config/          ScriptableObject assets (XpConfig, per-book scribe rosters, achievement sets, pricing curves, Grace Skill Tree)
  _Scenes/          MainMenu, ClickerScreen, BuyVerseScreen, PrestigeScreen, SettingsScreen, AchievementScreen,
                    StatsScreen, CreditsScreen, SaveSlotScreen, NewGameSetupScreen, CoreLoopTest (dev/smoke-test)
  _Audio/           Audio mixer
  _Fonts/           Ibarra Real Nova (openly licensed)
  _Prefabs/, _UI/, _Settings/     Prefabs, generated/UI-only sprites, render-pipeline settings
  _ThirdParty/      All imported art/UI asset packs, one subfolder per pack
  Resources/
    Bible/          Canonical KJV outline (all 66 books, chapter/verse counts)
    Verses/         Loaded verse text by book (all 39 Old Testament books staged)
  Plugins/          Native/managed plugin DLLs (Velopack, etc.) — Unity-reserved folder name
```

`Resources/`, `Plugins/`, `Editor/` (nested), `TextMesh Pro/`, and `AddressableAssetsData/` keep their exact Unity-required names; everything else lives under an underscore-prefixed category folder so the Project window sorts real project content above imported packages and package-managed folders.

## Getting started

1. Clone with LFS: `git lfs install && git clone https://github.com/neogentrics/Clicker-Genesis.git`
2. Open in Unity 6.5 (URP).
3. Open `Assets/_Scenes/MainMenu.unity` and press Play — this is the real production entry point (not `CoreLoopTest.unity`, which is a standalone dev/smoke-test scene for the core tap→buy loop only).

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
- NIV/Amplified translations (on hold — confirmed no legally usable public-domain edition exists for either); ASV, WEB, RV1909 (Spanish), and the Webster Bible are already extracted and validated as legally clear alternatives, not yet wired into gameplay
- Real audio content — purchase-click SFX are wired in as of 0.4.6 (Scribe/Manager/Support/Verse/Chapter buy buttons); music and voice clips still don't exist yet
- Real mobile-device responsive layout — as of 0.4.6 the app locks to landscape orientation at the OS level (portrait/live-rotation was built and verified working, but Unity's CanvasScaler can't correctly serve one layout in both orientations at once) — this resolves the worst of the earlier portrait-layout bugs by removing portrait entirely rather than continuing to chase per-orientation fixes; the Skill Tree's own zoom baseline (Bug #58) is still open
- New Testament content (27 books' worth of scribe/manager data, already imported and validated) is intentionally not switchable in-game yet — a real OT→NT transition system (separate skill tree, separate currency) is designed but not built, gated behind polishing the Old Testament experience first
- A full Geneva Bible 1560 + Ethiopian Bible data layer (231 books' worth of scribe/manager content indexed alongside KJV's — extracted, validated, not yet wired into gameplay)
- The desktop auto-update system hasn't yet been exercised through a real packaged install/update cycle — infrastructure compiles clean and is wired, but not click-tested end to end

## Guiding principle

Scripture text is never altered, paraphrased, or reinterpreted. Only unedited, recognized translations are used. This constraint overrides convenience in any implementation decision — it's the one rule in this project that doesn't get relaxed for the sake of shipping faster.
