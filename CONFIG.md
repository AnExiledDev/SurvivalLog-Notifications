# Notifications settings

Settings live in `BepInEx\config\com.dev.survivallog.notifications.cfg`, created the first time the game starts with the mod. Edit the file while the game is closed; changes apply at the next game start. Delete the file to get the defaults back.

How long toasts stay on screen and where they appear are set in Exiled's Toolkit (`Seconds`, 7 by default). When the Toolkit's `Enabled` is `false`, no toasts are drawn: every popup and message shows as the game draws it, and the phone's Notifications app still keeps a copy.

## Popups

| Setting | Default | What it does |
| --- | --- | --- |
| `WishFulfilled` | `true` | Show a toast instead of the popup when a wish comes true. Click the toast, or its entry in the phone's Notifications app, to open the popup. |
| `ItemsReceived` | `true` | Show a toast instead of the received-items popup (bumper harvests, first-time dishes, rewards). Click it to open the popup. The small pickup pops that fade by themselves stay as they are. The item pickup window with Pick All is never changed. |
| `WaveReport` | `true` | Show a toast instead of the zombie wave report. Click it to open the report. |
| `SkillLevelUp` | `true` | Show a toast instead of the skill level-up popup. Click it to open the popup. |
| `DroneDelivery` | `true` | When a drone comes home, show a "Drone delivery arrived" toast instead of the unload popup. Click it to open the popup. |

Set one to `false` to get that popup back as the game shows it. Popups that are switched off here are not kept in the phone list.

## Game toasts

| Setting | Default | What it does |
| --- | --- | --- |
| `PopText` | `true` | Show the game's short messages (such as not enough stamina) as toasts. Identical messages in a row merge into one toast with a count. |
| `SystemUnlock` | `true` | Show system unlocks as toasts. |
| `CharacterUnlock` | `true` | Show new survivor unlocks as toasts. |
| `KeepGameToastLook` | `false` | When `true`, the game draws its short messages and unlocks itself, as before, and they are only copied into the phone list. |

Every message these settings turn on goes into the phone's Notifications app, whether it shows as a toast or as the game draws it. Set one of the first three to `false` to leave that kind of message to the game alone and out of the list.
