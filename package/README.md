# Homeward

MMO-style recall for Valheim. Press a key, channel for a few seconds, wake up in your bed.

## What it does
- Teleports you to your claimed bed (same spawn point the game uses on death)
- Cooldown between uses (default 60 min, real time, survives logout)
- Channel time with cancel-on-damage (default 8 s)
- Respects the normal portal rules for metal (configurable)

## Config
`BepInEx/config/canpoy.homeward.cfg`

| Key | Default | Meaning |
|---|---|---|
| Hotkey | H | Key to start the journey home |
| CooldownMinutes | 60 | Real-time minutes between uses |
| CastSeconds | 8 | Channel time before teleport |
| AllowWithMetal | false | Ignore the portal metal restriction |

## Install
r2modman / Thunderstore Mod Manager. Needs BepInExPack_Valheim.

## Known issues
- (none yet, which means we haven't tested enough)
