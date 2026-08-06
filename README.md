# Clicker Genesis

A Bible-verse idle/incremental clicker for Android, built in Unity. Tap to earn Ink, spend Ink to unlock the next verse of Scripture in canonical order, and grow a roster of themed "scribes" that keep generating Ink while you're away — all wrapped around the goal of helping players actually **read and memorize the Bible** through play.

> Micro-transactions exist in the eventual design but are never required for progress or content. Scripture text is never altered, paraphrased, or reinterpreted — only unedited, recognized translations are used (KJV in v1). This constraint overrides convenience in every implementation decision.

<p align="center">
  <img src="docs/screenshots/clicker-screen.png" width="45%" alt="Clicker screen — tap for Ink, passive scribe income, milestone bonuses" />
  <img src="docs/screenshots/buy-verse-screen.png" width="45%" alt="Buy Verse screen — spend Ink on the next verse or bulk-buy a chapter" />
</p>

## Status

**Version 0.3.2** — pre-release, core loop implemented and playable, actively being stabilized through real-device playtesting. See [Versioning](#versioning) below for what that number means.

This is an active work-in-progress being built in public as part of a development portfolio — see [Bug Tracker](#bug-tracker--known-issues) for a transparent, running log of what's broken and what's been fixed.

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
- Run-in-background toggle (Settings) so idle Ink income keeps ticking while the window is unfocused
- Shared passive "progress multiplier": every 5 verses bought adds +0.1 to a multiplier applied across all scribe output, and it doubles outright on every chapter completed
- Real hover/press visual feedback on every button in the game, plus tooltips throughout
- Managers are correctly locked until their own scribe tier is unlocked by verse progress, not just by player level — and that gating stays keyed to Genesis' progress specifically, so switching to a different active book never re-locks already-unlocked scribes/managers

## Tech stack

- **Engine:** Unity 6.5, URP 3D Core template
- **Language:** C#, ScriptableObject-driven config (pricing curves, XP constants, scribe rosters — nothing gameplay-numeric is hardcoded, so it can be rebalanced after playtesting without touching code)
- **UI:** TextMeshPro, a custom-generated Georgia font asset (flagged for a licensing swap before any commercial release — see [Open questions](#open-questions--roadmap))
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
- Save/load persistence (currently in-memory-per-session only — explicitly deferred, not yet started)
- Real narrative-accurate scribe/manager unlock thresholds beyond the current placeholder mechanism-testing values
- A properly licensed serif font before any commercial release (Georgia is Windows-system-licensed, fine for prototyping only)
- NIV/Amplified translations (on hold pending licensing review)
- Real audio (volume slider, SFX/BGM, verse narration) — not started
- Save/load, Psalms placement, and font licensing are the main blockers before any release build

## Guiding principle

Scripture text is never altered, paraphrased, or reinterpreted. Only unedited, recognized translations are used. This constraint overrides convenience in any implementation decision — it's the one rule in this project that doesn't get relaxed for the sake of shipping faster.
