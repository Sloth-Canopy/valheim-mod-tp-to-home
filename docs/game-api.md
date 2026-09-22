# Verified Valheim API surface

Every game member the mod touches, verified against `decompiled/` from
**Valheim 1.0.15** (`Version.cs:168`, `CurrentVersion = new GameVersion(1, 0, 15)`).
Line numbers refer to files in `decompiled/`. Re-verify after any game update —
regenerate `decompiled/` (see dev-setup.md) and re-grep.

Rule: **don't call a game method that isn't in this table.** Grep it, add it, then use it.

## Player (`Player.cs`)

| Member | Line | Signature | Notes |
|---|---|---|---|
| `m_localPlayer` | 158 | `public static Player m_localPlayer` | Null in menus / before spawn. Check every frame. |
| `m_customData` | 581 | `public Dictionary<string,string> m_customData` | Persists in the character save. Our cooldown lives here. |
| `TeleportTo` | 5889 | `public override bool TeleportTo(Vector3 pos, Quaternion rot, bool distantTeleport)` | Returns `false` if not owner, already teleporting, or `m_teleportCooldown < 2f`. Returning `true` = started, not arrived. Use `distantTeleport: true` to get the loading screen + zone streaming. **Never set `transform.position` directly.** |
| `IsTeleporting` | 5968 | `public override bool IsTeleporting()` | Returns `m_teleporting`. Flips back to false on arrival → that's our "stamp the cooldown" moment. |
| `UpdateTeleport` | 5916 | `private void UpdateTeleport(float dt)` | Where the actual move happens. With `distantTeleport: true`: moves you after 2 s, waits for `IsAreaReady` + 8 s, then `FindFloor`; if no floor by 15 s it snaps you to `GetSolidHeight + 0.5`. **Never bounces you back** on distant teleports, so `IsTeleporting()` → false is a reliable arrival signal. |
| `InAttack` | 6991 | `public override bool InAttack()` | Channel-cancel condition. |
| `OnDamaged` | 6793 | `protected override void OnDamaged(HitData hit)` | Harmony postfix target for "took damage → cancel channel". Protected — patch by string name. |
| `StartEmote` | 6172 | `public bool StartEmote(string emote, bool oneshot = true)` | Lowercase name, e.g. `"sit"` (`Emote.cs:12` does `emote.ToString().ToLower()`). Returns `false` if `!CanMove() \|\| InAttack() \|\| IsDrawingBow() \|\| IsAttached()`. Writes to the ZDO; takes effect in `UpdateEmote` a frame later. |
| `StopEmote` | 6188 | `protected override void StopEmote()` | Protected — we call it via `AccessTools.Method(typeof(Player), "StopEmote")`. |
| `UpdateEmote` | 6198 | `private void UpdateEmote()` | **Stops the emote when `m_moveDir != Vector3.zero`** — i.e. any movement input. That's our free "moved" detector. |
| `InEmote` | 6231 | `public override bool InEmote()` | True while `m_emoteState` is set (looping emotes like sit). |
| `ShowTeleportAnimation` | 5973 | `public bool ShowTeleportAnimation()` | Returns `m_distantTeleport` while teleporting. `Hud.UpdateBlackScreen` shows the portal swirl only when this is true; a plain fade otherwise. We prefix-patch it to `false`. |
| `InPlaceMode` | 3675 | `public override bool InPlaceMode()` | Build mode. Cancel condition. |
| `InMinorAction` | 7120 | `public override bool InMinorAction()` | Eating / item-use animations. Cancel condition. |
| `IsDead` | 5829 | `public override bool IsDead()` | |
| `IsRiding` | 6459 | `public override bool IsRiding()` | Refuse to start the channel. |
| `IsAttached` | 6426 | `public override bool IsAttached()` | Chairs, ships, mounts. Refuse to start. |
| `Message` | 5388 | `public override void Message(MessageHud.MessageType type, string msg, int amount = 0, Sprite icon = null, bool log = false)` | Local-player-only wrapper around `MessageHud`. Use this instead of `MessageHud.instance` directly. |

## Humanoid (`Humanoid.cs`) — Player inherits this

| Member | Line | Signature | Notes |
|---|---|---|---|
| `IsTeleportable` | 2002 | `public bool IsTeleportable(bool allowAllItems)` | Delegates to `Inventory.IsTeleportable`. Pass our `AllowWithMetal` config directly. |
| `IsDrawingBow` | 1742 | `public override bool IsDrawingBow()` | Cancel condition. |
| `IsSitting` | 1478 | `public override bool IsSitting()` | `GetCurrentAnimHash() == s_animatorTagSitting` — the **animator** is in the sit state. Vanilla's own "am I sitting" test (`Player.cs:1154`: `InEmote() && IsSitting()`). `InEmote()` alone is true while swimming because it only reads the ZDO flag. **Reads the *current* animator state only** (`GetCurrentAnimHash`, `Character.cs:3702`) — the sit-down transition isn't tagged, so this stays false for ~1–2 s after `StartEmote`. Latch it; don't gate on a fixed delay. |
| `IsBlocking` | 1878 | `public override bool IsBlocking()` | Cancel condition. |

## Inventory (`Inventory.cs`)

| Member | Line | Signature | Notes |
|---|---|---|---|
| `IsTeleportable` | 1185 | `public bool IsTeleportable(bool allowAllItems)` | Items with `m_toolTier >= 1000` are *never* teleportable regardless of flag. Also returns true if world key `GlobalKeys.TeleportAll` is set. |

## Character (`Character.cs`) — base of Humanoid/Player

| Member | Line | Signature | Notes |
|---|---|---|---|
| `IsOnGround` | 2848 | `public bool IsOnGround()` | Refuse to start / cancel if false (jumping, falling). |
| `IsSwimming` | 3523 | `public bool IsSwimming()` | Refuse to start / cancel. `StartEmote("sit")` does **not** check this. |

## PlayerProfile (`PlayerProfile.cs`)

| Member | Line | Signature | Notes |
|---|---|---|---|
| `HaveCustomSpawnPoint` | 721 | `public bool HaveCustomSpawnPoint()` | Per-world: reads `GetWorldData(ZNet.instance.GetWorldUID()).m_haveCustomSpawnPoint`. |
| `GetCustomSpawnPoint` | 716 | `public Vector3 GetCustomSpawnPoint()` | Per-world. This is the bed position. Stays set even if the bed is destroyed. |

Get the profile via `Game.instance.GetPlayerProfile()`.

## Game (`Game.cs`)

| Member | Line | Signature |
|---|---|---|
| `instance` | 232 | `public static Game instance { get; private set; }` |
| `GetPlayerProfile` | 797 | `public PlayerProfile GetPlayerProfile()` |

## MessageHud (`MessageHud.cs`)

| Member | Line | Signature | Notes |
|---|---|---|---|
| `MessageType` | 8 | `enum { TopLeft = 1, Center }` | `Center` = big centered text (like "Rested"). |
| `ShowMessage` | 156 | `public void ShowMessage(MessageType type, string text, int amount = 0, Sprite icon = null, bool showDespiteHiddenHUD = false, bool log = true)` | Via `MessageHud.instance`. |

## Hud (`Hud.cs`) — the native action bar we borrow for the channel

| Member | Line | Signature | Notes |
|---|---|---|---|
| `instance` | 368 | `public static Hud instance` | |
| `UpdateActionProgress` | 803 | `private void UpdateActionProgress(Player player)` | Called every frame from `Hud.Update`. Hides `m_actionBarRoot` whenever the player's action queue is empty, so we **postfix** it and re-show the bar while channeling. |
| `m_actionBarRoot` | 144 | `public GameObject m_actionBarRoot` | |
| `m_actionProgress` | 146 | `public GuiBar m_actionProgress` | `SetValue(float)` takes a 0–1 fraction (`GuiBar.m_maxValue` defaults to 1). `GuiBar` lives in `assembly_guiutils.dll` → `decompiled-guiutils/GuiBar.cs:57`. |
| `m_actionName` | 148 | `public TMP_Text m_actionName` | Needs `Unity.TextMeshPro.dll` + `UnityEngine.UI.dll` references to compile. |
| `UpdateBlackScreen` | 571 | `private void UpdateBlackScreen(Player player, float dt)` | For reference: shows the loading screen while `IsTeleporting()`; picks portal swirl vs. plain black via `ShowTeleportAnimation()`. |

## Input guards — don't fire the hotkey while typing

| Class | Line | Signature |
|---|---|---|
| `Menu` | `Menu.cs:291` | `public static bool IsVisible()` |
| `Console` | `Console.cs:87` | `public static bool IsVisible()` |
| `TextInput` | `TextInput.cs:34` | `public static bool IsVisible()` |
| `Chat` | `Chat.cs:170` | `public bool HasFocus()` (instance: `Chat.instance`) |

## ZNet (`ZNet.cs`) — only if we ever switch to world time

| Member | Line | Signature |
|---|---|---|
| `instance` | 301 | `public static ZNet instance` |
| `GetTimeSeconds` | 2813 | `public double GetTimeSeconds()` |

## Bed (`Bed.cs`)

| Member | Line | Signature | Notes |
|---|---|---|---|
| `GetSpawnPoint` | 172 | `public Vector3 GetSpawnPoint()` | `m_spawnPoint.position` — the "stand here" transform beside the bed. This is what gets stored as the custom spawn point (`Bed.cs:67,105`), so the profile value is already a safe standing position. |

## Game respawn behaviour (`Game.cs:533` `FindSpawnPoint`) — for reference
On respawn, vanilla looks for a `Bed` within 5 m of the custom spawn point
(`FindBedNearby(point, 5f)`). If none, it **clears** the custom spawn point and
falls back to the start temple. We can't replicate this pre-teleport because the
target zone isn't loaded yet — see decisions.md #6.

## Not yet verified (Phase 3 — grep before using)
- `StatusEffect` / Jötunn `CustomStatusEffect` API
- ServerSync API
