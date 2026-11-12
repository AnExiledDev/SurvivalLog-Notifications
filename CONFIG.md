# Notifications settings

Settings live in `BepInEx\config\com.dev.survivallog.notifications.cfg`, created the first time the game starts with the mod. Change them in game on the Mod Manager's Mods screen, where they take effect at once, or edit the file while the game is closed. Delete the file to get the defaults back.

How long toasts stay on screen and where they appear are set in Exiled's Toolkit (`Seconds`, 7 by default). When the Toolkit's `Enabled` is `false`, no toasts show: every popup and message shows as usual, and the phone's Notifications app still keeps a copy.

## Popups

| Setting | Default | What it does |
| --- | --- | --- |
| `WishFulfilled` | `true` | Show a toast instead of the popup when a wish comes true. Click the toast, or its entry in the phone's Notifications app, to open the popup. |
| `ItemsReceived` | `true` | Show a toast instead of the received-items popup (bumper harvests, first-time dishes, rewards). Click it to open the popup. The small pickup messages that fade on their own don't change. The item pickup window with Pick All is never changed. |
| `WaveReport` | `false` | Show a toast instead of the zombie wave report. Click it to open the report. Off by default, so the wave report shows as a popup. |
| `SkillLevelUp` | `true` | Show a toast instead of the skill level-up popup. Click it to open the popup. |
| `DroneDelivery` | `true` | When a drone comes home, show a "Drone delivery arrived" toast instead of the unload popup. Click it to open the popup. |

Set one to `false` to get that popup back. Popups you switch off here are not kept in the phone list.

## Game toasts

| Setting | Default | What it does |
| --- | --- | --- |
| `PopText` | `true` | Show the game's short messages (such as not enough stamina) as toasts. When the same message comes several times in a row, you get one toast with a count. |
| `SystemUnlock` | `true` | Show system unlocks as toasts. |
| `CharacterUnlock` | `true` | Show new survivor unlocks as toasts. |
| `KeepGameToastLook` | `false` | When `true`, the game shows its short messages and unlocks as usual, and the mod only copies them into the phone list. |

## Basket

| Setting | Default | What it does |
| --- | --- | --- |
| `BasketBack` | `true` | Show a "The basket is back" toast when your neighbor's or companion's basket comes back with her return gifts, and keep it in the phone list. |

Every message these settings turn on goes into the phone's Notifications app, whether it shows as a toast or the usual way. If you set `PopText`, `SystemUnlock` or `CharacterUnlock` to `false`, the game shows that kind of message as usual and it stays out of the list.
