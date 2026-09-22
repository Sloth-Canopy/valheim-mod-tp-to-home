# Build plan

Phases in order. Tick things as they land. Play with each phase for a night
before starting the next — the instant-teleport version will tell you whether
60 min feels right before you build the channel.

## Phase 0 — Setup ✅ (2026-09-21)
- [x] .NET 10 SDK installed in WSL (`dotnet-sdk-10.0` via apt; 8.0 isn't in Ubuntu 26.04 repos)
- [x] `ilspycmd` installed as a dotnet global tool, `~/.dotnet/tools` on PATH
- [x] `assembly_valheim.dll` decompiled → `decompiled/`
- [x] API surface verified against decompiled source → `docs/game-api.md`
- [x] Located BepInEx/Harmony/Jötunn DLLs in the r2modman profile → `docs/dev-setup.md`
- [x] `src/Homeward.csproj` scaffolded (net472, references, post-build copy to r2modman profile + `package/`)
- [x] Empty plugin loads and logs "Homeward 0.1.0 loaded" in `BepInEx/LogOutput.log` (confirmed 2026-09-21)

## Phase 1 — MVP: press key, go home
- [x] BepInEx config: `Hotkey` (KeyboardShortcut, default H), `CooldownMinutes` (60), `AllowWithMetal` (false)
- [x] `Update()`: bail if no local player / `Menu.IsVisible()` / `Console.IsVisible()` / `TextInput.IsVisible()` / `Chat.instance.HasFocus()`
- [ ] On hotkey:
  - [x] `HaveCustomSpawnPoint()` else message "You have no home."
  - [x] cooldown check via `m_customData` else "Homeward ready in mm:ss"
  - [x] `IsTeleportable(AllowWithMetal)` else "Cannot travel home while carrying metal."
  - [x] `IsTeleporting()` → ignore
  - [x] `TeleportTo(spawnPoint, player.transform.rotation, distantTeleport: true)`
- [x] Stamp cooldown after arrival (see decisions #7)
- [x] Message feedback via `player.Message(MessageHud.MessageType.Center, ...)`
- [ ] Test in single player (code written + deployed 2026-09-21, untested)

## Phase 2 — The channel
- [ ] Coroutine on hotkey: "Heading home..." countdown, configurable `CastSeconds` (default 8)
- [ ] Cancel on: moved > tolerance, `InAttack()`, took damage
- [ ] Damage detection: Harmony postfix on `Player.OnDamaged(HitData)` → sets a flag
- [ ] Teleport fires only when the channel completes
- [ ] Optional: reuse portal VFX/SFX from the `portal_wood` prefab (`ZNetScene.instance.GetPrefab`)

## Phase 3 — Polish
- [ ] Jötunn `CustomStatusEffect` for the cooldown icon in the buff bar
- [ ] ServerSync: server-enforced `CooldownMinutes` / `AllowWithMetal` / `CastSeconds`
- [ ] Config: `CancelOnDamage` toggle
- [ ] Remaining-cooldown text on early press

## Phase 4 — Ship (see `docs/publishing.md`)
- [ ] `package/` filled in: `manifest.json`, `README.md`, `CHANGELOG.md`, 256×256 `icon.png`
- [ ] Local-import test in r2modman passes (Stage 1 in publishing.md)
- [ ] Profile-export test with friends (Stage 2)
- [ ] Test on a dedicated server with a friend (multiplayer haunting check)
- [ ] Publish

## Known gotchas (collected as we go)
- `TeleportTo` has a built-in `m_teleportCooldown < 2f` guard — it silently returns `false` within 2 s of a previous teleport.
- `TeleportTo` returning `true` means *started*, not *arrived*. Actual movement happens over later frames in `UpdateTeleport`.
- `OnDamaged` is `protected override` on `Player` — fine for Harmony, just needs `[HarmonyPatch(typeof(Player), "OnDamaged")]` with a string name (or publicized assembly + `nameof`).
- The game's `IsTeleportable` also honours the world modifier `GlobalKeys.TeleportAll`, so worlds with "teleport everything" enabled will Just Work.
