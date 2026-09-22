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
- [x] BepInEx config: `Hotkey` (KeyboardShortcut, default H), `CooldownSeconds` (3600), `AllowWithMetal` (false)
- [x] `Update()`: bail if no local player / `Menu.IsVisible()` / `Console.IsVisible()` / `TextInput.IsVisible()` / `Chat.instance.HasFocus()`
- [x] On hotkey:
  - [x] `HaveCustomSpawnPoint()` else message "You have no home."
  - [x] cooldown check via `m_customData` else "Homeward ready in mm:ss"
  - [x] `IsTeleportable(AllowWithMetal)` else "Cannot travel home while carrying metal."
  - [x] `IsTeleporting()` → ignore
  - [x] `TeleportTo(spawnPoint, player.transform.rotation, distantTeleport: true)`
- [x] Stamp cooldown after arrival (see decisions #7)
- [x] Message feedback via `player.Message(MessageHud.MessageType.Center, ...)`
- [x] Test in single player (verified 2026-09-21 — note: bed must be *claimed*, which needs a roof + ≥80% cover; an unclaimed bed correctly yields "You have no home.")

## Phase 2 — The channel ✅ (verified 2026-09-22)
- [x] `CastSeconds` config (default 8, 0 = instant), `CancelOnDamage`, `ShowPortalAnimation`
- [x] Channel uses the game's **sit emote** (`StartEmote("sit")`) — player visibly sits on the floor
- [x] Cancel on: moved (emote dropped or position drifted > 0.5), attack / bow / block, item use (`InMinorAction`), build mode, death, damage (Harmony postfix on `Player.OnDamaged`)
- [x] Hotkey again while channeling cancels
- [x] Native action bar shows "Heading home... Ns" (postfix on `Hud.UpdateActionProgress`)
- [x] Portal swirl replaced with a plain fade to black (prefix on `Player.ShowTeleportAnimation`)
- [x] Teleport fires only when the channel completes; cooldown still stamps on arrival
- [x] Test in single player: sit animation, bar counts down, cancel reasons fire, fade is plain black, arrival stamps cooldown (verified 2026-09-22)
- [x] Swimming exploit fixed (2026-09-22): channel now requires `IsOnGround && !IsSwimming && !IsRiding && !IsAttached` to start, and `InEmote() && IsSitting()` (the real animation) after a 1 s grace
- [x] "Interrupted (not sitting)" false positive fixed (2026-09-22): `IsSitting()` is false during the sit-down transition, so the seated check now latches on first sight and only then enforces; 3 s ceiling if it never seats
- [ ] Decide whether the emote grace (0.5 s) / seat timeout (3 s) need tuning on a laggy server
- ~~Reuse portal VFX/SFX~~ — dropped; the fade-to-black is the effect now

## Phase 3 — Polish
- [x] Cooldown icon in the buff bar (2026-09-22) — a `StatusEffect` subclass added straight via `SEMan.AddStatusEffect(instance)`; **no Jötunn needed**. Bed icon borrowed from the `bed` prefab, grey cooldown overlay, live `m:ss`, "Homeward is ready." when it expires
- [ ] Test: icon appears on arrival, counts down, survives logout/login and death, disappears + message at 0
- [x] Channeling VFX v1 (2026-09-22) — borrowed vanilla status-effect visuals. Rejected: didn't look like channeling.
- [x] Channeling VFX v2 (2026-09-22) — **our own**: rune ring quad on the ground (embedded PNG, spins, fades in) + code-built particle motes rising from the rim. `Visuals` config: enabled / color / radius
- [ ] Test v2: ring appears flat under the player, spins, motes rise, both vanish on cancel/complete; check the log line `Channel VFX shader: ...`
- [ ] Tune: color, radius, spin speed, mote density — by eye
- [ ] Multiplayer: the VFX is local-only (no ZNetView). Decide if other players should see it (would need a registered prefab + RPC)
- [ ] ServerSync: server-enforced `CooldownSeconds` / `AllowWithMetal` / `CastSeconds`
- [x] Config: `CancelOnDamage` toggle (landed in Phase 2)
- [x] Remaining-cooldown text on early press (landed in Phase 1)

## Phase 4 — Ship (see `docs/publishing.md`)
- [ ] `package/` filled in: `manifest.json`, `README.md`, `CHANGELOG.md`, 256×256 `icon.png`
- [ ] Local-import test in r2modman passes (Stage 1 in publishing.md)
- [ ] Profile-export test with friends (Stage 2)
- [ ] Test on a dedicated server with a friend (multiplayer haunting check)
- [ ] Publish

## Known gotchas (collected as we go)
- `Player.StartEmote("sit")` returns `true` while swimming / mid-air and `InEmote()` stays true, but the animator never sits. Check `IsSitting()` (animator tag) for the truth — that's what vanilla does. But it reads the *current* state, and the sit-down transition state isn't tagged, so it lags `StartEmote` by a second or two. Latch it rather than using a fixed grace.
- `TeleportTo` has a built-in `m_teleportCooldown < 2f` guard — it silently returns `false` within 2 s of a previous teleport.
- `TeleportTo` returning `true` means *started*, not *arrived*. Actual movement happens over later frames in `UpdateTeleport`.
- `OnDamaged` is `protected override` on `Player` — fine for Harmony, just needs `[HarmonyPatch(typeof(Player), "OnDamaged")]` with a string name (or publicized assembly + `nameof`).
- The game's `IsTeleportable` also honours the world modifier `GlobalKeys.TeleportAll`, so worlds with "teleport everything" enabled will Just Work.
