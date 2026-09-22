# Homeward — Valheim "teleport to bed" mod

An MMO-style recall for Valheim. Press a key, channel for a few seconds,
get teleported to your claimed bed. 60-minute cooldown (configurable).

**Status:** 0.4.0 — channeling VFX (2026-09-22), awaiting in-game test alongside the 0.3.0 cooldown icon.

**Repo:** https://github.com/Sloth-Canopy/valheim-mod-tp-to-home (private)
API is verified. Next step is writing the Phase 1 plugin.

## Layout

```
README.md            this file
CLAUDE.md            context for Claude Code sessions in this repo
docs/
  plan.md            phased build plan (what we're doing, in what order)
  decisions.md       design decisions and why
  game-api.md        verified Valheim API surface we depend on (file:line refs)
  dev-setup.md       toolchain, paths, how to decompile, gotchas
  publishing.md      Thunderstore package format, local testing, upload, tcli
package/             Thunderstore package template (manifest, README, changelog; icon TODO)
decompiled/          ilspycmd output of assembly_valheim.dll (gitignored, regenerable)
decompiled-guiutils/ same for assembly_guiutils.dll (GuiBar)
src/                 the mod; `dotnet build -c Release` deploys
  HomewardPlugin.cs  BepInEx entry point, config, hotkey
  Channel.cs         the sit-still channel + cancel rules + departure
  Flight.cs          in-flight tracking, stamps cooldown on arrival
  Cooldown.cs        real-time cooldown in Player.m_customData
  Patches.cs         Harmony patches (damage, portal swirl, action bar)
  CooldownEffect.cs  buff-bar icon; a StatusEffect that mirrors the cooldown
  ChannelEffect.cs   hidden StatusEffect that carries a borrowed vanilla VFX while channeling
```

## Quick facts

| | |
|---|---|
| Game version | Valheim 1.0.15 |
| Mod loader | BepInEx 5.4.23.x (via r2modman profile `canpoy-mods`) |
| Patching | HarmonyX (bundled with BepInEx) |
| Helper lib | none — BepInEx + Harmony only |
| Target framework | `net472` |
| Build SDK | .NET 10 SDK in WSL (`dotnet --version` → 10.0.112) |

## Docs to read first

1. `docs/decisions.md` — so you don't re-argue settled stuff
2. `docs/plan.md` — where we are
3. `docs/game-api.md` — before touching any game class
