# Homeward (valheim-mod-tp-to-home)

Valheim BepInEx/Harmony mod: hotkey teleport to claimed bed with a cooldown.
C# / netstandard2.1 (Unity 6 API profile) / Unity Mono. Built from WSL with the .NET 10 SDK.

## Read before working
- `docs/plan.md` — phased plan and current phase
- `docs/decisions.md` — settled design choices; don't relitigate without asking
- `docs/game-api.md` — verified game API. **Every game method we call is listed
  there with a file:line ref into `decompiled/`.** If you need a new game method,
  grep `decompiled/` and add it to that doc before using it.
- `docs/dev-setup.md` — paths, toolchain, how to regenerate `decompiled/`
- `docs/publishing.md` — Thunderstore packaging + release checklist

## Rules
- Verify game signatures in `decompiled/` — never from memory. Valheim updates
  break method names.
- `decompiled/` is gitignored game code. Never commit it.
- Mod DLLs deploy to the r2modman profile, NOT the Steam game folder
  (see dev-setup.md, "leftover BepInEx" gotcha).
- Keep docs current: when a decision changes, update `decisions.md`; when a
  phase completes, tick it in `plan.md`.
