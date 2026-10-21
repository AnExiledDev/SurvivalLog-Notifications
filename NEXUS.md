Name: Notifications
Summary: Stop the game interrupting you for things that only need a glance. Wishes coming true, items received, skill level-ups and drone deliveries show as toasts instead of popups, and every notice is kept in a Notifications app on your phone with the day and hour. Click a toast to open the original popup. Needs Exiled's Toolkit.

Description (BBCode):
Survival Log stops play with a popup for many things that only need a glance: a wish coming true, items received, a skill level-up, a drone unloading. Its short messages (such as not enough stamina) flash by and are easy to miss. Notifications shows all of these as toasts instead, and keeps a full list of them on the phone, so nothing is lost if you looked away.

[size=5]Main features[/size]
[list]
[*][b]Popups become toasts[/b]: wishes fulfilled, items received, skill level-ups and drone deliveries show a toast instead of stopping play. Each can be switched back to the popup on its own. The zombie wave report stays the game's own popup unless you turn on WaveReport.[/*]
[*][b]Click to open[/b]: clicking the toast opens the original popup, with the same contents.[/*]
[*][b]The game's short messages too[/b]: short messages, system unlocks and new survivor unlocks show as toasts that stay on screen longer. They can also be left to the game while still being kept on the phone.[/*]
[*][b]Repeats merge[/b]: the same message several times in a row shows one toast with a count (x2, x3, ...).[/*]
[*][b]Phone app[/b]: a Notifications app on the phone lists every notice of the current save, newest first, with the game day and hour. A dot on the icon shows when there is something new.[/*]
[*][b]Reopen from the list[/b]: clicking a notice in the list opens its popup again, when it had one.[/*]
[*][b]Kept per save[/b]: up to the newest 500 notices, back when you load that save again.[/*]
[/list]
Popups that ask you to choose something, such as events and the item pickup window with Pick All, are never changed.

Notifications adds nothing to your save and does not edit game files. The list is kept in its own files next to the mod's settings. You can remove it at any time.

Works in English and Chinese.

[size=5]Known issues[/size]
[list]
[*]After a restart, a notice in the list can open its popup again only if the popup can be rebuilt from what was saved.[/*]
[*]When some of a drone's cargo does not fit, it stays in the drone as before, and the toast has no popup to open.[/*]
[*]The list starts empty for saves played before the mod was installed.[/*]
[/list]

[size=5]Requirements[/size]
[list]
[*]Survival Log 1.1.19131. A game update can break it until the mod is updated.[/*]
[*][url=https://www.nexusmods.com/survivallog/mods/12]BepInEx for Survival Log[/url]. Install it first.[/*]
[*]Exiled's Toolkit 0.4.0 or newer, which draws the toasts and applies changed settings at once. Notifications does not load without it.[/*]
[/list]

[size=5]Installation[/size]
[list=1]
[*]Install [url=https://www.nexusmods.com/survivallog/mods/12]BepInEx[/url] [i](if no other mods were installed before, start the game once so BepInEx finishes setup, then quit)[/i].[/*]
[*]Download the zip from the Files tab.[/*]
[*]Extract it into the game folder (the folder with [font=Courier New]SurvivalLog.exe[/font]). The zip holds a BepInEx folder, so the DLL lands in the right place. When asked, merge the folders. The result should be:
[code]C:\Program Files (x86)\Steam\steamapps\common\Survival Log\BepInEx\plugins\Notifications.dll[/code][/*]
[/list]

[size=5]Uninstall[/size]
Delete [font=Courier New]Notifications.dll[/font] from the [font=Courier New]BepInEx\plugins[/font] folder.

[size=5]Configuration[/size]
To choose which popups and messages become toasts, edit [font=Courier New]BepInEx\config\com.dev.survivallog.notifications.cfg[/font] while the game is closed, or do it in game on the Mod Manager's Mods screen, where changes take effect at once (see [url=https://github.com/AnExiledDev/SurvivalLog-Notifications/blob/main/CONFIG.md]CONFIG.md[/url]; the cfg file explains each setting in English and Chinese). Where the toasts appear and how long they stay are Exiled's Toolkit settings.

[size=5]Feedback[/size]
Found a bug or have an idea? Come say hi on the [url=https://discord.gg/trTsyxUFqg]Core Directive Discord[/url], or post in this page's Posts tab. For bugs, please include [font=Courier New]BepInEx\LogOutput.log[/font] and [font=Courier New]BepInEx\notifications.log[/font].
To reach me directly, message [b]exiled.dev[/b] on Discord; you can find me in the [url=https://discord.gg/MZGg4gQAUe]official Survival Log Discord[/url]. I'm also on X as [url=https://x.com/AnExiledDev]@AnExiledDev[/url].
Coming soon: a Survival Log wiki with detailed documentation for all of my mods.

[size=5]Source and license[/size]
Open source under MIT: [url=https://github.com/AnExiledDev/SurvivalLog-Notifications]github.com/AnExiledDev/SurvivalLog-Notifications[/url]

[line]
[size=5]简体中文[/size]

Survival Log 会为许多只需看一眼的事情弹出窗口、打断游戏：愿望满足、获得物品、熟练度提升、无人机卸货。它的简短提示（例如精力不足）一闪而过，很容易错过。Notifications 把这些都改为提示显示，并在手机上保存完整的列表，即使你没看到也不会丢失。

[size=5]主要功能[/size]
[list]
[*][b]弹窗变成提示[/b]：愿望满足、获得物品、熟练度提升和无人机送货都显示为提示，不再打断游戏。每一项都可以单独改回弹窗。尸潮战报默认仍是游戏自带的弹窗，开启 WaveReport 后才改为提示。[/*]
[*][b]点击打开[/b]：点击提示会打开原来的弹窗，内容不变。[/*]
[*][b]游戏的简短提示也一样[/b]：简短提示、系统解锁和新幸存者解锁显示为停留更久的提示。也可以仍由游戏自己显示，同时记入手机。[/*]
[*][b]重复合并[/b]：连续出现的相同提示只显示一条，并带有次数（x2、x3 等）。[/*]
[*][b]手机应用[/b]：手机上的「通知」应用列出当前存档的每条通知，最新的在前，并显示游戏中的日期和时刻。有新通知时，图标上会出现小圆点。[/*]
[*][b]从列表重新打开[/b]：点击列表中的通知，可以再次打开它的弹窗（如果有）。[/*]
[*][b]按存档保存[/b]：最多保留最新的 500 条通知，再次读取该存档时会回来。[/*]
[/list]
需要你做选择的弹窗，例如事件和带有全部拾取按钮的拾取窗口，永远不会被改动。

Notifications 不会往存档里写入任何内容，也不会修改游戏文件。列表保存在模组设置旁边它自己的文件里。你可以随时移除它。

支持英文和中文。

[size=5]已知问题[/size]
[list]
[*]重启游戏后，只有能根据保存的内容重建的弹窗，才能从列表中再次打开。[/*]
[*]无人机的部分货物放不下时，会像以前一样留在无人机里，此时提示没有可打开的弹窗。[/*]
[*]安装模组之前玩过的存档，列表从空开始。[/*]
[/list]

[size=5]前置要求[/size]
[list]
[*]Survival Log 1.1.19131。游戏更新后模组可能暂时失效，直到模组更新为止。[/*]
[*][url=https://www.nexusmods.com/survivallog/mods/12]BepInEx for Survival Log[/url]，请先安装。[/*]
[*]Exiled's Toolkit 0.4.0 或更新版本，用于显示通知，并让改动的设置立即生效。没有它，Notifications 无法加载。[/*]
[/list]

[size=5]安装[/size]
[list=1]
[*]安装 [url=https://www.nexusmods.com/survivallog/mods/12]BepInEx[/url] [i]（如果之前没有装过其他模组，先启动一次游戏，让 BepInEx 完成初始化，然后退出）[/i]。[/*]
[*]在「文件」页下载压缩包。[/*]
[*]把压缩包解压到游戏目录（[font=Courier New]SurvivalLog.exe[/font] 所在的文件夹）。压缩包里带有 BepInEx 文件夹，DLL 会自动放到正确位置。如果提示合并文件夹，选择合并。解压后应为：
[code]C:\Program Files (x86)\Steam\steamapps\common\Survival Log\BepInEx\plugins\Notifications.dll[/code][/*]
[/list]

[size=5]卸载[/size]
从 [font=Courier New]BepInEx\plugins[/font] 文件夹中删除 [font=Courier New]Notifications.dll[/font]。

[size=5]配置[/size]
如需选择哪些弹窗和提示改为通知，可以在游戏内 Mod Manager 的「模组」界面修改（改动立即生效），也可以在游戏关闭时编辑 [font=Courier New]BepInEx\config\com.dev.survivallog.notifications.cfg[/font]。配置文件中每项设置都有中文说明，完整说明见 [url=https://github.com/AnExiledDev/SurvivalLog-Notifications/blob/main/CONFIG.md]CONFIG.md[/url]（英文）。

[size=5]反馈[/size]
发现 bug 或有好点子？欢迎来 [url=https://discord.gg/trTsyxUFqg]Core Directive Discord[/url] 打个招呼，或在本页的「Posts」标签里发帖。报告 bug 时，请附上 [font=Courier New]BepInEx\LogOutput.log[/font] 和 [font=Courier New]BepInEx\notifications.log[/font]。
想直接联系我，可以在 Discord 上给 [b]exiled.dev[/b] 发消息，我在 [url=https://discord.gg/MZGg4gQAUe]Survival Log 官方 Discord[/url] 里。也可以在 X 上找我：[url=https://x.com/AnExiledDev]@AnExiledDev[/url]。
即将推出：Survival Log 维基，收录我所有模组的详细说明。

[size=5]源代码与许可[/size]
基于 MIT 许可开源：[url=https://github.com/AnExiledDev/SurvivalLog-Notifications]github.com/AnExiledDev/SurvivalLog-Notifications[/url]
