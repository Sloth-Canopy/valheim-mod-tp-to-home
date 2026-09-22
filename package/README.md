# Homeward

MMO-style recall for Valheim. Press a key, channel for a few seconds, wake up in your bed.

## What it does
- Teleports you to your claimed bed (same spawn point the game uses on death)
- Cooldown between uses (default 60 min, real time, survives logout)
- You sit down to channel (default 8 s) with a progress bar; moving, attacking, using an item, or taking damage cancels it
- Respects the normal portal rules for metal (configurable)
- Cooldown shows as a buff icon with a live countdown
- A glowing rune ring appears under you while channeling — other players see it too — with a gentle sound only you hear (all configurable)

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
| ChannelVfxEnabled | true | Rune ring + motes under you while channeling |
| ChannelVfxColor | #7FD7FF | Tint of the ring and motes (HTML color) |
| ChannelVfxRadius | 1.2 | Ring radius in meters |
| ChannelVfxVisibleToOthers | true | Other players with the mod see your ring (never your sound) |
| ChannelSoundEnabled | true | Gentle drone-and-chimes loop while channeling |
| ChannelSoundVolume | 0.3 | Sound volume (also scaled by the SFX slider) |

## Install
r2modman / Thunderstore Mod Manager. Needs BepInExPack_Valheim.

## Known issues
- (none yet, which means we haven't tested enough)
