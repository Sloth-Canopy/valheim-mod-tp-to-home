# Homeward

MMO-style recall for Valheim. Press a key, channel for a few seconds, wake up in your bed.

## What it does
- Teleports you to your claimed bed (same spawn point the game uses on death)
- Cooldown between uses (default 60 min, real time, survives logout)
- You sit down to channel (default 8 s) with a progress bar; moving, attacking, using an item, or taking damage cancels it
- Respects the normal portal rules for metal (configurable)

## Config
`BepInEx/config/canpoy.homeward.cfg`

| Key | Default | Meaning |
|---|---|---|
| Hotkey | H | Key to start the journey home (press again to cancel) |
| CooldownMinutes | 60 | Real-time minutes between uses |
| CastSeconds | 8 | Seconds you sit still before the teleport fires (0 = instant) |
| AllowWithMetal | false | Ignore the portal metal restriction |
| CancelOnDamage | true | Taking damage cancels the channel |
| ShowPortalAnimation | false | Show the vanilla portal swirl instead of a plain fade |

## Install
r2modman / Thunderstore Mod Manager. Needs BepInExPack_Valheim.

## Known issues
- (none yet, which means we haven't tested enough)
