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
| Where our DLL goes | `<profile>/BepInEx/plugins/Homeward/Homeward.dll` (post-build copy) |
| BepInEx log | `<profile>/BepInEx/LogOutput.log` — first place to look when nothing happens |

Other mods in the profile: PlantEverything, FreeFuelSource, EquipmentAndQuickSlots,
RossItemDrawers. Good to know for conflict-hunting later.

## Toolchain

| Tool | Version | Install |
|---|---|---|
| .NET SDK | 10.0.112 | `sudo apt install dotnet-sdk-10.0` (8.0 is not in the 26.04 repos; any SDK ≥ 6 can build `net472`) |
| ilspycmd | 11.0.0 | `dotnet tool install -g ilspycmd` |
| PATH | | `export PATH="$PATH:$HOME/.dotnet/tools"` is in `~/.bashrc` |

The mod targets **`net472`** because that's what Valheim's Mono runtime speaks.
The SDK version and the target framework are independent — SDK 10 compiles a
net472 library fine, it just needs the reference assemblies package
(`Microsoft.NETFramework.ReferenceAssemblies`, pulled in automatically by the
SDK when you target net472).

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

## Gotchas

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

## Build / deploy loop (once `src/` exists)

```bash
cd src && dotnet build -c Release
# post-build target copies Homeward.dll into the r2modman profile
# then: launch Valheim through r2modman (profile: canpoy-mods), tail LogOutput.log
```
