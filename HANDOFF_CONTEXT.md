# Handoff — paste this into the Unity-rooted session

Note: this project's `CLAUDE.md` only auto-loads for sessions that *start* after it existed. The "Tap-to-buy verse core loop scaffolding" session was already running before `CLAUDE.md` was written, so paste this whole file into that chat to bring it current — don't assume it already knows this.

## What this is
Design/planning work for Clicker Genesis happened in a separate chat (rooted at `I:\n8n Projects`, no Unity Editor access) up through project creation and the MCP handoff. This file is the bridge. `CLAUDE.md` in this same folder has the stable reference version of the confirmed design — this file adds the narrative of how we got here and what's actually in flight right now.

## Full design doc (source of truth going forward)
https://app.notion.com/p/3aead37812e881c99578d596f3a29921
Keep it updated when design decisions get made or changed — it's the cross-session record, not this file.

## The conversation so far, in order

1. **Concept:** Bible-verse idle clicker for Android, goal is helping players learn/memorize scripture, not just a numbers-go-up game. IAP exists but is never required for progress or content.
2. Landed on the **Scribe/manuscript theme**: tap → Ink → "buy" the next verse in canonical order → verse text + reference reveal on screen.
3. **Translations:** KJV (public domain, safe), NIV + Amplified (licensed — need commercial-use permission confirmed before building around them, not yet cleared).
4. **Book unlock flow:** player freely picks ONE starting book on first run (free) — personalization hook. Locked to that book until prestige-eligible.
5. **Pricing:** rejected flat pricing. `verse_cost(n) = base_cost × growth_rate^n`, `n` resets per newly-unlocked book. Chapter bulk-buy = sum of verse costs × 0.75. Constants must be config-driven, not hardcoded — real tuning happens after playtesting.
6. **XP:** comes only from actions already in the loop (verse/chapter/book completion), no separate leveling or achievement subsystem for v1.
7. **Prestige — this took the most back-and-forth, now CONFIRMED:**
   - Free path: cross the level threshold, nothing is lost (Ink, upgrades, unlocked verses all stay), unlocks next-book-purchase screen, grants normal permanent-currency reward ("Grace," working name). Costs creep up slightly per prestige tier — that's the natural difficulty scaling.
   - Optional reset path: voluntarily wipe Ink + upgrade levels for 2–3x the permanent currency.
   - **Non-negotiable rule on both paths:** a reset never re-locks or removes verses already unlocked/read. Only numbers reset, never delivered scripture. This exists specifically to protect the memorization goal — taking scripture away from someone who's already started learning it would undercut the whole point of the game.
8. **Per-book currency/theming:** the "different colored Ink per book" idea (66 books = plenty of room, per the Bible's book count) is the *long-term* vision, explicitly phased: v1 ships one shared Ink currency with cosmetic per-book reskinning only; real separate non-transferable per-book economies + unique clicker sets are v1.x/v2, after the base loop is proven fun.
9. **Psalms is still an open question** — longest book (150 chapters) but wildly uneven chapter length (Psalm 1 is ~6 verses). Live options: offer it as a starter book (short early chapters = friendly on-ramp) vs. gate deep Psalms progress behind prestige (natural "main progression spine" given its size). Needs real per-chapter verse-count data across the Bible before deciding — flagged as a data task, not a vibes call.
10. **Versioning scheme (confirmed):** `X.Y.Z` — X = official release marker (0 = pre-release, bumps at real public launches), Y = feature/update bump (whether or not that build ships live), Z = patch/bugfix only.
11. **Project setup:** Unity 6.5, switched from HD 3D (wrong — HDRP has no real mobile support) to **URP 3D Core** — correction made *before* project creation, not after. GitHub repo `clicker-genesis`, private, Git LFS on. Org: Dark Cancer Gaming (placeholder).
12. **Unity-MCP (IvanMurzak/Unity-MCP via ai-game.dev)** installed via `npx unity-mcp-cli install-plugin`, then authorized inside the Editor. Confirmed live: `.mcp.json` in this folder points to `https://ai-game.dev/mcp/p/ed1997c3`, and a direct curl to the local bridge port returned a valid MCP JSON-RPC response.
13. Started scaffolding C# for the core loop in the other (non-Unity) session before the MCP handoff was finished: created `Assets/Scripts/{Data,Economy,Progression,Core,Editor}` folders, intent was Ink wallet, pricing-curve math, and a small real Genesis 1:1–10 (KJV, public domain) sample dataset so there's something to actually press Play on. **Check what, if anything, landed in those folders before assuming a blank slate** — the folder structure exists but the C# files inside may or may not have been written yet depending on timing.

## Immediate next step
Verify what's actually in `Assets/Scripts/` right now, then finish (or start) the Ink wallet + pricing curve + sample verse data + a minimal test scene wiring so the tap → buy-verse loop is actually playable end to end with placeholder/round numbers.

## Open questions still waiting on the user
- NIV/Amplified commercial licensing status.
- Psalms: starter book vs. prestige-gate (needs per-chapter data pulled first).
- Exact cost to unlock the second book after prestige.
- Studio/publisher name + which Play Store dev account to publish under.
- IAP scope: cosmetics-only vs. also a patronage/support tier.
