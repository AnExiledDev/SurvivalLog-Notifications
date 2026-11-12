# Notifications

Turns the game's interrupting popups into toasts, and keeps every notice in a Notifications app on your phone.

Survival Log stops play with a popup for many things that only need a glance: a wish coming true, items received, a skill level-up, a drone unloading. Its short messages (such as not enough stamina) flash by at the bottom of the screen and are easy to miss. Notifications shows all of these as toasts in a corner of the screen instead, and keeps a full list of them on the phone, so nothing is lost if you looked away.

## Features

- **Popups become toasts**: wishes coming true, items received, skill level-ups and drone deliveries show a toast instead of stopping play. You can switch each one back to a popup. The zombie wave report still shows as a popup unless you turn on `WaveReport`.
- **Click to open**: click a toast to open the popup it replaced, with everything that popup would have shown.
- **Short messages and unlocks**: the game's short messages, system unlocks and new survivor unlocks show as toasts, which are bigger and stay on screen longer. You can also let the game show them as usual and still keep a copy on the phone.
- **Basket return**: you get a toast when your neighbor's or companion's basket comes back with her return gifts. Without the mod, the game only sends a text message.
- **Repeated messages merge**: when the same message comes several times in a row, you get one toast with a count (x2, x3, ...) instead of a stack of copies. Only messages that match exactly are merged.
- **Phone app**: a Notifications app on the phone's home screen lists every notice from the current save, newest first and grouped by game day. Each notice shows where it came from, the hour, and how many times it came in. Filters let you show only new notices, popups, messages, or notices from other mods. The app icon shows how many notices are new, and the home screen shows the newest three.
- **Details in the list**: click a notice in the list to see its details, such as the items you got and how many, or what a wish gave you. If the notice came from a popup, Open popup closes the phone and shows that popup again.
- **Read and clear**: a notice stays new until you open it. Mark all read marks every notice as read, and Clear list empties the list for this save after asking you first.
- **Kept with your save**: each save has a separate list that keeps the newest 500 notices, and it comes back when you load that save again.

Popups that ask you to choose something, such as events and the item pickup window with Pick All, are never changed.

Notifications adds nothing to your save and does not edit game files. The lists are kept in separate files next to the mod's settings. You can remove the mod at any time.

## Requirements

- Survival Log 1.1.20719. A game update can break the mod until it's updated.
- The [BepInEx Pack for Survival Log](https://www.nexusmods.com/survivallog/mods/12), the BepInEx 6 (IL2CPP) build for the game.
- **Exiled's Toolkit** 0.19.0 or newer, which draws the toasts and applies changed settings at once. Notifications does not load without it.
- Optional: [Mod Manager](https://www.nexusmods.com/survivallog/mods/73). With it, you can change every setting in game on its Mods screen and see what each one does, without opening the config file.

## Install

1. Install the [BepInEx Pack for Survival Log](https://www.nexusmods.com/survivallog/mods/12) (if no other mods were installed before, start the game once so BepInEx finishes setup, then quit).
2. Install Exiled's Toolkit: download `ExiledToolkit-x.y.z.zip` from [its latest release](https://github.com/AnExiledDev/SurvivalLog-ExiledToolkit/releases/latest) or from [Nexus Mods](https://www.nexusmods.com/survivallog/mods/61), and extract it into the game folder (the folder with `SurvivalLog.exe`).
3. Download `Notifications-x.y.z.zip` from the [latest release](https://github.com/AnExiledDev/SurvivalLog-Notifications/releases/latest) or from [Nexus Mods](https://www.nexusmods.com/survivallog/mods/62).
4. Extract it into the game folder the same way. It holds a `BepInEx` folder, so the DLL lands in the right place. When asked, merge the folders. The result should be:
   `C:\Program Files (x86)\Steam\steamapps\common\Survival Log\BepInEx\plugins\Notifications.dll`

## Uninstall

Delete `Notifications.dll` from the `BepInEx\plugins` folder. For a clean slate, also delete `BepInEx\config\com.dev.survivallog.notifications.cfg`, the `BepInEx\config\notifications` folder (the saved lists) and `BepInEx\notifications.log`.

## Configuration

Settings live in `BepInEx\config\com.dev.survivallog.notifications.cfg`, created the first time the game starts with the mod. Change them in game on the Mod Manager's Mods screen, where they take effect at once, or edit the file while the game is closed. Each setting in the cfg file is explained in English and Simplified Chinese. See [CONFIG.md](CONFIG.md) for every setting.

The phone app and the mod's own toast text follow the game's language: English or Simplified Chinese.

How long toasts stay on screen, which corner they use, and whether they show at all are Exiled's Toolkit settings. With the Toolkit's toasts turned off, every popup shows as usual and the phone list still keeps a copy of everything.

## Known issues and limits

- After a restart, a notice in the list can open its popup again only if the mod saved enough to rebuild that popup.
- A notice from another mod can only be opened until you quit the game.
- When some of a drone's cargo does not fit, that cargo stays in the drone as before, and the toast has no popup to open.
- The list starts empty for saves played before the mod was installed.

## Troubleshooting

Check `BepInEx\LogOutput.log` for `===== Notifications 0.4.8 started`. The same lines, and anything that went wrong, are in `BepInEx\notifications.log`, which survives restarts.

If a saved list can't be read, the mod renames it with a `.bad` ending in `BepInEx\config\notifications` and starts a new list, so the old one is never written over.

## Contact

To reach me directly, message `exiled.dev` on Discord. You can find me in the [official Survival Log Discord](https://discord.gg/MZGg4gQAUe). I'm also on X as [@AnExiledDev](https://x.com/AnExiledDev).

A Survival Log wiki with detailed documentation for all of my mods is coming soon.
