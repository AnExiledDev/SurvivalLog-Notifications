# Notifications

Turns the game's interrupting popups into toasts, and keeps every notice in a Notifications app on your phone.

Survival Log stops play with a popup for many things that only need a glance: a wish coming true, items received, a zombie wave report, a skill level-up, a drone unloading. Its short messages (such as not enough stamina) flash by at the bottom of the screen and are easy to miss. Notifications shows all of these as toasts in a corner of the screen instead, and keeps a full list of them on the phone, so nothing is lost if you looked away.

## Features

- **Popups become toasts**: wishes fulfilled, items received, zombie wave reports, skill level-ups and drone deliveries show a toast instead of stopping play. Each can be switched back to the popup on its own.
- **Click to open**: clicking the toast opens the original popup, with the same contents it would have shown.
- **The game's short messages too**: the game's own short messages, system unlocks and new survivor unlocks show as toasts, which are bigger and stay on screen longer than the game's. They can also be left to the game while still being kept on the phone.
- **Repeats merge**: the same message sent several times in a row shows one toast with a count (x2, x3, ...) instead of a stack of copies. Only messages that are exactly the same merge.
- **Phone app**: a Notifications app on the phone's home screen lists every notice of the current save, newest first, with where it came from, the game day and hour, and how many times it came. A dot on the icon shows when there is something new.
- **Reopen from the list**: clicking a notice in the list closes the phone and opens its popup again, when it had one.
- **Kept with your save**: the list is kept per save, up to the newest 500 notices, and comes back when you load that save again.

Popups that ask you to choose something, such as events and the item pickup window with Pick All, are never changed.

Notifications adds nothing to your save and does not edit game files. The list is kept in its own files next to the mod's settings. You can remove the mod at any time.

## Requirements

- Survival Log 1.1.18317. Built and tested on this version; a game update can break it until the mod is updated.
- The [BepInEx Pack for Survival Log](https://www.nexusmods.com/survivallog/mods/12), the BepInEx 6 (IL2CPP) build for the game.
- **Exiled's Toolkit**, which draws the toasts. Notifications does not load without it.

## Install

1. Install the [BepInEx Pack for Survival Log](https://www.nexusmods.com/survivallog/mods/12) (if no other mods were installed before, start the game once so BepInEx finishes setup, then quit).
2. Install Exiled's Toolkit: put `ExiledToolkit.dll` in the `BepInEx\plugins` folder inside the game folder.
3. Put `Notifications.dll` in the same `BepInEx\plugins` folder. Full path example:
   `C:\Program Files (x86)\Steam\steamapps\common\Survival Log\BepInEx\plugins\Notifications.dll`

## Uninstall

Delete `Notifications.dll` from the `BepInEx\plugins` folder. For a clean slate, also delete `BepInEx\config\com.dev.survivallog.notifications.cfg`, the `BepInEx\config\notifications` folder (the saved lists) and `BepInEx\notifications.log`.

## Configuration

Settings live in `BepInEx\config\com.dev.survivallog.notifications.cfg`, created the first time the game starts with the mod. Edit it while the game is closed; changes apply at the next game start. See [CONFIG.md](CONFIG.md) for every setting.

How long toasts stay on screen, which corner they use, and whether they are drawn at all are Exiled's Toolkit settings. With the Toolkit's toasts turned off, every popup shows as the game draws it and the phone list still keeps a copy of everything.

## Known issues and limits

- A notice in the list can open its popup again after a restart only if the popup can be rebuilt from what was saved. A notice from another mod's toast can be opened only in the session it arrived in.
- When some of a drone's cargo does not fit, it stays in the drone as before, and the toast has no popup to open.
- The list starts empty for saves played before the mod was installed.

## Troubleshooting

Check `BepInEx\LogOutput.log` for `===== Notifications 0.1.2 started`. The same lines, and anything that went wrong, are in `BepInEx\notifications.log`, which survives restarts.

If a saved list cannot be read, it is moved aside with a `.bad` ending in `BepInEx\config\notifications` and a new list starts, so the old one is never written over.
