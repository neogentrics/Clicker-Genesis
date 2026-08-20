# Contributing

This is a one-person project built in public. Contributions are welcome, and
so is simply reporting that something is broken — that is genuinely the most
useful thing most people can do here.

## The fastest way to help

**Play it and tell me what broke.** Nearly every bug fixed in this project was
found by someone using it on hardware I do not own. A bug report with your
platform, the version number from the main menu, and what you expected to
happen is worth more than a patch.

## Reporting a bug

Use the [bug report template](../../issues/new?template=bug_report.yml). It
asks for the things that actually determine whether a bug is reproducible:

- **Platform and version.** The version is on the main menu, bottom-left.
- **What you did, what happened, what you expected.**
- **A screenshot**, if it is a visual bug. These help more than any
  description — several layout bugs here were diagnosed straight from one.

Bugs are tracked with a running number in an offline tracker as well as here,
so an issue may be referenced as `#120` in a commit while being a different
number on GitHub. The issue body states the mapping when it differs.

## Suggesting a feature

Use the [feature request template](../../issues/new?template=feature_request.yml).

Two things worth knowing before you write it:

- **The scripture text is never altered, paraphrased or reinterpreted.** Only
  unedited, recognised translations are used. This constraint overrides
  convenience, and a suggestion that requires breaking it will be declined
  however good it is otherwise.
- **Purchases are never required for progress or content.** Anything buyable
  must also be reachable through ordinary play.

## Pull requests

Open an issue first for anything larger than a small fix, so neither of us
wastes effort on an approach the other would not take.

- Branch from `main`.
- Match the surrounding code. This project comments the *why*, not the *what* —
  if a line needs explaining, explain the reasoning behind it, not its syntax.
- One logical change per PR.
- Say how you tested it, and on what.

### Things to know about this codebase

- **Unity 6.5, URP.** Scenes live in `Assets/_Scenes/` (desktop) and
  `Assets/_ScenesAndroid/` (Android) as *separate files with independent
  GUIDs*. A layout change to one must be mirrored into the other in the same
  PR. Logic-only C# changes need no mirroring.
- **Wire UI in `Awake()`, not `Start()`.** `Start()` has proven unreliable
  under this project's automation; `Awake()` is safe either way.
- **Numbers are placeholders pending playtesting.** Costs, curves and output
  values are data-driven ScriptableObject fields on purpose. Do not hardcode
  them, and do not "balance" them without playtest data.

## What is not accepted

- Anything that alters, paraphrases or reinterprets scripture text.
- AI-generated art or audio assets for the game itself. Third-party assets are
  bought and credited; that is the standard here.
- Content that makes a purchase required for progress.

## Code of conduct

By participating you agree to the [Code of Conduct](CODE_OF_CONDUCT.md).
