# Homeward

MMO-style recall for Valheim. Press a key, channel for a few seconds, wake up in your bed.

## What it does
- Teleports you to your claimed bed (same spawn point the game uses on death)
- Cooldown between uses (default 60 min, real time, survives logout)
- You sit down to channel (default 8 s) with a progress bar; moving, attacking, using an item, or taking damage cancels it
- Respects the normal portal rules for metal (configurable)
- Cooldown shows as a buff icon with a live countdown
- A visual effect surrounds you while channeling (configurable)

## Config
`BepInEx/config/canpoy.homeward.cfg`

| Key | Default | Meaning |
|---|---|---|
| Hotkey | H | Key to start the journey home (press again to cancel) |
| CooldownSeconds | 3600 | Real-time seconds between uses (3600 = 1 hour) |
| CastSeconds | 8 | Seconds you sit still before the teleport fires (0 = instant) |
| AllowWithMetal | false | Ignore the portal metal restriction |
| CancelOnDamage | true | Taking damage cancels the channel |
| ShowPortalAnimation | false | Show the vanilla portal swirl instead of a plain fade |
| ChannelVfx | Spirit | Vanilla status-effect visual shown while channeling (Spirit, Lightning, Frost, Burning, Poison, Shield, None) |

## Install
r2modman / Thunderstore Mod Manager. Needs BepInExPack_Valheim.

## Known issues
- (none yet, which means we haven't tested enough)
