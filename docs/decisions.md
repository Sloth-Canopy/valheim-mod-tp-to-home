# Design decisions

Settled choices. If you want to change one, change it here first and say why.

| # | Decision | Choice | Why |
|---|---|---|---|
| 1 | Cooldown clock | **Real time (UTC unix seconds)**, not in-game time | "60 min" should mean 60 min. Logging out must not reset it. In-game time only advances while the world runs. |
| 2 | Cooldown storage | `Player.m_customData["homeward.lastUsed"]` as a `long` string | Sanctioned mod-data bag, persists in the character save. Per-character, across worlds. **Never** store as `DateTime.ToString()` — culture formatting bug waiting to happen. |
| 3 | Cast time | **8 s channel as the sit emote**, cancel on damage / attack / movement / item use / build mode | It's the recall feel. Instant is boring and a combat-escape cheese. The sit emote gives a visible "channeling" pose for free and the game already cancels it on movement input. **You can only go home from where you can actually sit** — no swimming, mid-air, riding, or chairs — verified via the animator (`IsSitting()`), not just the emote flag. Configurable; 0 = instant. |
| 4 | Metal / ore in inventory | **Blocked by default**, config `AllowWithMetal` | Portals block it; a recall that ignores it makes portals pointless. The game's own `IsTeleportable(bool allowAllItems)` already takes this as a parameter — pass the config straight in. |
| 5 | No bed claimed | **Refuse** with a message | Do not fall back to the start temple. |
| 6 | Bed destroyed | **Teleport to the stale spawn point anyway** | Profile still holds it. Vanilla respawn *does* check for a bed within 5 m and clears the point if missing, but only once the zone is loaded — we can't check pre-teleport. Acceptable: you arrive at a crater. Could add a post-arrival bed check in Phase 3 if it bugs us. |
| 7 | Cooldown stamp timing | Write **after arrival** (`IsTeleporting()` flips back to `false`), not on cast start or on `TeleportTo()` returning `true` | `TeleportTo` returning `true` only means "started". A cancelled/failed teleport must not burn the cooldown. |
| 8 | Jötunn | **Not used at all** (revised 2026-09-22) | Was planned for the status-effect icon, but `SEMan.AddStatusEffect` accepts a raw instance so no registry is needed. Zero dependencies beyond BepInEx. Revisit only if ServerSync/UI work actually needs it. |
| 9 | Multiplayer trust | Client-authoritative for v1; **ServerSync in Phase 3** | Valheim teleports are owner-authoritative anyway. ServerSync lets an admin enforce cooldown/metal config for everyone. |
| 10 | Default hotkey | `H` via BepInEx `KeyboardShortcut` config | Guarded by menu/console/chat/text-input checks so it doesn't fire while typing. |
| 11 | Spawn point scope | Per-world (game already does this: `GetWorldData(worldUID).m_spawnPoint`) | Nothing to decide, just noting it — your bed in world A is not your bed in world B. |
| 12 | Name | **Homeward** (was "Hearth", renamed 2026-09-21) | Plain, searchable, says what it does. Avoids "Hearthstone" (Blizzard trademark) entirely, and avoids colliding with Valheim's own "Hearth" build piece on Thunderstore search. Runners-up: Heimleid, Hamfare. |

| 14 | Loading screen | **Plain fade to black**, no portal swirl (config `ShowPortalAnimation` to restore) | A seamless teleport isn't possible — the destination zone has to stream in and you'd watch terrain pop. The fade reads as "close your eyes, wake up at home", which fits the bed better than the portal effect does. |
| 15 | Channel UI | **Borrow the game's action bar** (`Hud.m_actionBarRoot`) instead of custom UI | Native look, zero art, no Jötunn dependency. Cost: a Harmony postfix on a private Hud method, re-verified each game update. |

| 16 | Channeling VFX | **Custom, built at runtime**: rune-ring quad + code-configured `ParticleSystem`, textures embedded in the DLL (revised 2026-09-22; v1 borrowed vanilla status-effect VFX and was rejected on looks) | No Unity Editor, no asset bundle — iteration is build → relaunch. Textures are generated procedurally by `tools/gen_textures.py`, so there's no art pipeline either. Local-only for now. If we ever want custom shaders or meshes, that's the asset-bundle route (decision pending). |

| 17 | Channeling sound | **Synthesized at runtime** (drone + chimes into an `AudioClip` via `SetData`) | Same philosophy as the textures: no assets, no decoder, tunable by numbers. 3D-positioned on the ring, borrowed vanilla mixer group so the SFX slider applies. |
| 18 | Target framework | **`netstandard2.1`**, not `net472` | Unity 6's Audio/ImageConversion modules are netstandard 2.1; net472 can't reference them. BepInEx/Harmony (net35) still work through facades. Verified 2026-09-22: 0.5.0 loads and runs in-game. |

## Open questions
- Should the cooldown be per-world too (key by world UID) or per-character (current)? Leaning per-character. Revisit after playing with it.
- Cancel-on-movement tolerance: how far is "moved"? Start with 0.1 units and tune.
