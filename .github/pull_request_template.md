## What this changes

<!-- One or two sentences. What and why, not a file list. -->

## Why

<!-- The problem this solves. Link the issue if there is one: Fixes #123 -->

## How it was tested

<!-- Which platform(s), and what you actually did. "Builds fine" is not testing. -->

- [ ] Windows
- [ ] macOS
- [ ] Linux
- [ ] Android
- [ ] Editor Play mode only

## Checklist

- [ ] Scenes: if this changed layout in `Assets/_Scenes/`, the matching scene in `Assets/_ScenesAndroid/` was mirrored in this same PR (logic-only C# changes do not need this)
- [ ] No scripture text was altered, paraphrased or reinterpreted
- [ ] No numeric constants were hardcoded that belong in a ScriptableObject
- [ ] UI wiring is in `Awake()`, not `Start()`
- [ ] No third-party asset added without a credits entry

## Screenshots

<!-- Required for anything visual. Before/after if you are changing existing UI. -->
