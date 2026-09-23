# Publishing — Thunderstore & r2modman

## The one thing to understand first

**r2modman doesn't host anything.** It's the client. The registry it pulls from
is **Thunderstore** (https://thunderstore.io/c/valheim/). You publish a package
to Thunderstore; r2modman (and Thunderstore Mod Manager, and Gale) then show it
to everyone. "Publishing to r2modman" = "publishing to Thunderstore".

There are three stages, and you can stop at any of them:

| Stage | Who can install it | How |
|---|---|---|
| 1. Local | just you | r2modman → Settings → **Import local mod** (zip) |
| 2. Friends | your server group | r2modman **profile export code** (they import it; the local mod rides along) |
| 3. Public | everyone | upload the zip to Thunderstore |

## Package anatomy

A Thunderstore package is a **zip** with these at the root (not in a subfolder):

```
Homeward.zip
├── manifest.json     required — see below
├── README.md         required — this is the mod page on Thunderstore
├── icon.png          required — exactly 256×256 PNG
├── CHANGELOG.md      optional but everyone appreciates it
└── Homeward.dll        the mod. r2modman extracts the whole zip into
                      BepInEx/plugins/<Author>-Homeward/ so DLLs can sit at root.
```

Template lives at `package/` in this repo. The build should drop `Homeward.dll`
in there, then zip the folder contents.

### manifest.json

```json
{
  "name": "Homeward",
  "version_number": "1.0.0",
  "website_url": "https://github.com/<you>/valheim-mod-tp-to-home",
  "description": "MMO-style recall for Valheim. Hotkey teleport to your bed, on a cooldown.",
  "dependencies": [
    "denikson-BepInExPack_Valheim-5.4.2350"
  ]
}
```

Rules that will reject your upload if you get them wrong:
- `name`: letters, numbers, underscores only. **No spaces, no hyphens.** Max 128 chars.
- `version_number`: strict `MAJOR.MINOR.PATCH`. No `v` prefix, no `-beta`.
- `description`: max 250 chars.
- `dependencies`: `<Team>-<PackageName>-<Version>` strings. Exact versions; Thunderstore
  resolves "this or newer" on the client side. Use the versions you actually built against.
- `website_url` can be `""` but shouldn't be.

Dependency strings we'll use (from `mods.yml` in the `canpoy-mods` profile, 2026-09-21):
- BepInEx: `denikson-BepInExPack_Valheim-5.4.2350`
- Jötunn (Phase 3+ only): `ValheimModding-Jotunn-2.30.2`

Don't declare Jötunn as a dependency until we actually reference it — r2modman
installs every declared dependency, and pulling a library you don't use is rude.

### icon.png
256×256, PNG, that's the whole rule. Thunderstore rejects anything else.
Something viking-ish: a longhouse, a bed, a raven heading home. Doesn't have to be good; the
Jötunn icon is a stock render and nobody cares.

### README.md
Markdown, rendered as the mod page. The good ones have: one-line pitch,
what it does, config table (key / default / meaning), install note
("install via r2modman"), known issues, changelog link. Ours can be short.

## Stage 1 — Test the package locally

Do this before every upload; it catches "forgot to bump the version" and
"zipped the folder instead of the contents" (a classic; the mod silently
doesn't load).

1. Build → `package/Homeward.dll` present
2. `python3 tools/pack.py` → `dist/Homeward-<version>.zip` (files at the zip root; no `zip` binary needed)
3. r2modman → `canpoy-mods` profile → **Settings** → **Import local mod** → pick the zip
4. Launch modded, check `BepInEx/LogOutput.log` for `[Info   :   BepInEx] Loading [Homeward 1.0.0]`
5. To update: bump `version_number`, rebuild, re-import. r2modman keys local mods on name+version.

During development you *don't* need this loop — the post-build copy into
`BepInEx/plugins/Homeward/` is faster. Local import is for testing the *package*,
not the code.

**Gotcha:** the post-build copy puts the DLL in the profile *behind r2modman's
back*. r2modman doesn't list it, and **profile export won't include it**. To
share with friends you must import the zip as a local mod first (Stage 1), then
export. After importing, r2modman manages `BepInEx/plugins/<Author>-Homeward/`
(`Sloth-Homeward` here) — point `DeployDir` in `src/Homeward.local.props` at it
so builds replace the copy the game loads, and remove any hand-copied
`plugins/Homeward/` folder so two copies don't load.

## Stage 2 — Give it to friends without publishing

r2modman → profile → **Export profile as a code** (or as a file). Friends do
**Import profile** → code. Locally-imported mods are bundled into the export,
so this works for unpublished mods. (Hand-copied DLLs are not — see the gotcha
above.) Simplest alternative: just send them the zip and have them do
**Import local mod** themselves. The dedicated server does **not** need the mod;
it only forwards the routed RPCs. Good for a "does it work on the server with
four people" test before going public.

## Stage 3 — Publish to Thunderstore

### One-time
1. Account at https://thunderstore.io — log in with GitHub or Discord
2. Create a **Team** (Settings → Teams). The team name becomes the `<Author>` prefix:
   package `Homeward` under team `CanPoy` is installed as `CanPoy-Homeward`. Team
   names can't be changed later, pick one you can live with.

### Each release — web upload (fine for v1)
1. https://thunderstore.io/package/create/ (or "Upload" in the header)
2. Pick team, pick community **Valheim**, pick categories (Utility / Tweaks / Server-side if ServerSync)
3. Drop the zip. It validates manifest/icon/README and shows errors inline.
4. Submit. It's live in about a minute. **There is no unpublish** — you can
   deprecate a package, but versions are forever. Test before you upload.

### Each release — CLI (nicer once it's routine)
[tcli](https://github.com/thunderstore-io/thunderstore-cli) is a dotnet global tool:

```bash
dotnet tool install -g tcli
tcli init                      # writes thunderstore.toml in the project
tcli build                     # builds the zip from thunderstore.toml + package/
tcli publish --token "$TS_TOKEN"   # service account token from Team settings
```

`thunderstore.toml` replaces `manifest.json` as the source of truth (tcli
generates the manifest). The token is a **service account** token from your
team page, not your login — keep it out of git.

### Versioning
- Bump `version_number` every upload; Thunderstore rejects duplicates.
- Match the `[BepInPlugin(guid, name, version)]` attribute version in code to the
  manifest version. Nobody enforces it; everyone gets annoyed when they drift.
- Add a `CHANGELOG.md` line per release. Future You will read it. He's a good guy.

## Checklist before hitting Upload

- [ ] `version_number` bumped in manifest **and** `[BepInPlugin]`
- [ ] `dependencies` match what the DLL actually references
- [ ] `icon.png` is 256×256
- [ ] zip has `manifest.json` at the **root** (not `Homeward/manifest.json`)
- [ ] `strings package/Homeward.dll | grep -E '/home/|/Users/|\.pdb'` prints nothing (no build-machine paths)
- [ ] Local-import test passed on the `canpoy-mods` profile
- [ ] README config table matches the actual config keys
- [ ] CHANGELOG has an entry
