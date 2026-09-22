# Design decisions

Settled choices. If you want to change one, change it here first and say why.

| # | Decision | Choice | Why |
|---|---|---|---|
| 1 | Cooldown clock | **Real time (UTC unix seconds)**, not in-game time | "60 min" should mean 60 min. Logging out must not reset it. In-game time only advances while the world runs. |
| 2 | Cooldown storage | `Player.m_customData["homeward.lastUsed"]` as a `long` string | Sanctioned mod-data bag, persists in the character save. Per-character, across worlds. **Never** store as `DateTime.ToString()` — culture formatting bug waiting to happen. |
| 3 | Cast time | **~8 s channel**, cancel on damage / attack / movement | It's the hearthstone feel. Instant is boring and a combat-escape cheese. Cast time configurable. |
| 4 | Metal / ore in inventory | **Blocked by default**, config `AllowWithMetal` | Portals block it; a recall that ignores it makes portals pointless. The game's own `IsTeleportable(bool allowAllItems)` already takes this as a parameter — pass the config straight in. |
| 5 | No bed claimed | **Refuse** with a message | Do not fall back to the start temple. |
| 6 | Bed destroyed | **Teleport to the stale spawn point anyway** | Profile still holds it. Vanilla respawn *does* check for a bed within 5 m and clears the point if missing, but only once the zone is loaded — we can't check pre-teleport. Acceptable: you arrive at a crater. Could add a post-arrival bed check in Phase 3 if it bugs us. |
| 7 | Cooldown stamp timing | Write **after arrival** (`IsTeleporting()` flips back to `false`), not on cast start or on `TeleportTo()` returning `true` | `TeleportTo` returning `true` only means "started". A cancelled/failed teleport must not burn the cooldown. |
| 8 | Jötunn | **Not used in Phase 1–2**; use in Phase 3 for the status-effect cooldown icon | Raw BepInEx + Harmony covers everything until we want UI. Jötunn 2.30.2 is already in the r2modman profile so the dependency is free when we need it. |
| 9 | Multiplayer trust | Client-authoritative for v1; **ServerSync in Phase 3** | Valheim teleports are owner-authoritative anyway. ServerSync lets an admin enforce cooldown/metal config for everyone. |
| 10 | Default hotkey | `H` via BepInEx `KeyboardShortcut` config | Guarded by menu/console/chat/text-input checks so it doesn't fire while typing. |
| 11 | Spawn point scope | Per-world (game already does this: `GetWorldData(worldUID).m_spawnPoint`) | Nothing to decide, just noting it — your bed in world A is not your bed in world B. |
| 12 | Name | **Homeward** (was "Hearth", renamed 2026-09-21) | Plain, searchable, says what it does. Avoids "Hearthstone" (Blizzard trademark) entirely, and avoids colliding with Valheim's own "Hearth" build piece on Thunderstore search. Runners-up: Heimleid, Hamfare. |

## Open questions
- Should the cooldown be per-world too (key by world UID) or per-character (current)? Leaning per-character. Revisit after playing with it.
- Cancel-on-movement tolerance: how far is "moved"? Start with 0.1 units and tune.
