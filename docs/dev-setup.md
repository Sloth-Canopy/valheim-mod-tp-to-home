# Dev setup

Everything runs from WSL (Ubuntu 26.04). The game and r2modman live on the
Windows side and are reached through `/mnt/c`.

## Paths

| What | Path |
|---|---|
| Valheim install | `/mnt/c/Program Files (x86)/Steam/steamapps/common/Valheim/` |
| Game assemblies | `<Valheim>/valheim_Data/Managed/` — `assembly_valheim.dll`, `assembly_utils.dll`, `UnityEngine.*.dll` |
| r2modman profile (the one that actually runs) | `/mnt/c/Users/<windows-user>/AppData/Roaming/r2modmanPlus-local/Valheim/profiles/<profile>/` — set in `src/Homeward.local.props` (gitignored, see the `.example`) |
| BepInEx core DLLs | `<profile>/BepInEx/core/` — `BepInEx.dll`, `0Harmony.dll`, `Mono.Cecil.dll` |
| Jötunn | `<profile>/BepInEx/plugins/ValheimModding-Jotunn/Jotunn.dll` (2.30.2) |
| Where our DLL goes | `<profile>/BepInEx/plugins/Sloth-Homeward/Homeward.dll` — the folder r2modman created on local import; set as `DeployDir` in `src/Homeward.local.props` (post-build copy) |
| BepInEx log | `<profile>/BepInEx/LogOutput.log` — first place to look when nothing happens |

Other mods in the profile: PlantEverything, FreeFuelSource, EquipmentAndQuickSlots,
RossItemDrawers. Good to know for conflict-hunting later.

## Toolchain

| Tool | Version | Install |
|---|---|---|
| .NET SDK | 10.0.112 | `sudo apt install dotnet-sdk-10.0` (8.0 is not in the 26.04 repos; any SDK ≥ 6 can build `net472`) |
| ilspycmd | 11.0.0 | `dotnet tool install -g ilspycmd` |
| PATH | | `export PATH="$PATH:$HOME/.dotnet/tools"` is in `~/.bashrc` |

The mod targets **`netstandard2.1`** (changed from `net472` on 2026-09-22).
Valheim runs on Unity 6, whose Mono exposes the .NET Standard 2.1 API profile,
and several `UnityEngine.*Module.dll`s (Audio, ImageConversion, ...) are built
against netstandard 2.1 — a net472 project can't reference them (`CS1705`).
BepInEx and Harmony are net35 and still resolve fine through the standard
facades. The SDK version is unrelated: SDK 10 builds a netstandard2.1 library
with nothing extra.

## Regenerating `decompiled/`

```bash
ilspycmd -p -o decompiled \
  "/mnt/c/Program Files (x86)/Steam/steamapps/common/Valheim/valheim_Data/Managed/assembly_valheim.dll"
```

Produces ~630 `.cs` files. It's gitignored. Do this after every Valheim update,
then re-verify `docs/game-api.md`.

`GuiBar` and other UI helpers live in a second assembly:
```bash
ilspycmd -p -o decompiled-guiutils \
  "/mnt/c/Program Files (x86)/Steam/steamapps/common/Valheim/valheim_Data/Managed/assembly_guiutils.dll"
```

Handy greps:
```bash
grep -nE 'public .* TeleportTo\(' decompiled/Player.cs
grep -rnE 'HaveCustomSpawnPoint' decompiled/ | head
```

## Textures

`src/Resources/*.png` are generated, not drawn: `python3 tools/gen_textures.py`
(pure Python, no PIL). They're embedded in the DLL via `<EmbeddedResource>` and
loaded at runtime by `ChannelVfx`. Re-run the script after tweaking the art, then
rebuild. To preview alpha-only art, composite it on black (see the script's
comments / git history for a one-off preview snippet).

## Gotchas

**Unity 6 modules vs net472.** If you ever retarget to net472, referencing
`UnityEngine.AudioModule` / `ImageConversionModule` fails with `CS1705`
(netstandard 2.1) and then `CS0518` (`ReadOnlySpan`). The game's `netstandard.dll`
and `System.Memory.dll` are facades the compiler can't use. That's why the
project is netstandard2.1 — don't go back.


**Leftover BepInEx in the game folder.** The Steam install dir contains
`winhttp.dll`, `doorstop_config.ini` (enabled, pointing at
`BepInEx\core\BepInEx.Preloader.dll`) and a BepInEx `changelog.txt` — but no
`BepInEx/` directory. Doorstop finds nothing and the game runs vanilla. It's
harmless, but: **dropping a DLL into `<Valheim>/BepInEx/plugins/` does nothing**
because that folder isn't the one that runs. Always deploy to the r2modman
profile. If you ever want to run modded *without* r2modman, either delete the
leftovers or do a real BepInEx install there.

**Publicizing.** `Player.OnDamaged` is `protected`; other internals are
`private`. Two ways around it:
1. Harmony string-name patches: `[HarmonyPatch(typeof(Player), "OnDamaged")]` — works, no tooling, no compile-time safety.
2. `BepInEx.AssemblyPublicizer.MSBuild` NuGet package — publicizes referenced
   game DLLs at build time so `nameof(Player.OnDamaged)` compiles. Nicer once
   we have more than one patch. Decide in Phase 2.

**Unity version.** Check `<Valheim>/valheim_Data/` if we ever need the editor
for AssetBundles (Phase 3+). Not needed for a teleport mod.

## Testing without waiting an hour

The cooldown default lives in code (`CooldownSeconds = 3600`). For testing, change
it in the **local cfg only** — `<profile>/BepInEx/config/canpoy.homeward.cfg` —
e.g. `CooldownSeconds = 5`. The cfg isn't in the repo, so there's nothing to
remember to revert before pushing.

## Build / deploy loop

```bash
cd src && dotnet build -c Release
# post-build target copies Homeward.dll into the r2modman profile
# then: launch Valheim through r2modman (profile: canpoy-mods), tail LogOutput.log
```
