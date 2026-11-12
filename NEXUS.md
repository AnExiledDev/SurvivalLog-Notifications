Name: Notifications
Summary: Stop the game from interrupting you for things that only need a glance. Wishes coming true, items received, skill level-ups and drone deliveries show as toasts instead of popups, and every notice is kept in a Notifications app on your phone with the day and hour. Click a toast to open the original popup. Needs Exiled's Toolkit.

Description (BBCode):
[center][img]https://sldb.core-directive.com/media/save/1-save-safe.png[/img][/center]

Survival Log stops play with a popup for many things that only need a glance: a wish coming true, items received, a skill level-up, a drone unloading. Its short messages (such as not enough stamina) flash by and are easy to miss. Notifications shows all of these as toasts instead, and keeps a full list of them on the phone, so nothing is lost if you looked away.

[size=5]Main features[/size]
[list]
[*][b]Popups become toasts[/b]: wishes coming true, items received, skill level-ups and drone deliveries show a toast instead of stopping play. You can switch each one back to a popup. The zombie wave report still shows as a popup unless you turn on WaveReport.[/*]
[*][b]Click to open[/b]: click a toast to open the popup it replaced, with everything that popup would have shown.[/*]
[*][b]Short messages and unlocks[/b]: the game's short messages, system unlocks and new survivor unlocks show as toasts, which are bigger and stay on screen longer. You can also let the game show them as usual and still keep a copy on the phone.[/*]
[*][b]Basket return[/b]: you get a toast when your neighbor's or companion's basket comes back with her return gifts. Without the mod, the game only sends a text message.[/*]
[*][b]Repeated messages merge[/b]: when the same message comes several times in a row, you get one toast with a count (x2, x3, ...) instead of a stack of copies.[/*]
[*][b]Phone app[/b]: a Notifications app on the phone lists every notice from the current save, newest first and grouped by game day, with the hour. Filters let you show only new notices, popups, messages, or notices from other mods. The app icon shows how many notices are new, and the home screen shows the newest three.[/*]
[*][b]Details in the list[/b]: click a notice in the list to see its details, such as the items you got and how many, or what a wish gave you. If the notice came from a popup, Open popup shows that popup again.[/*]
[*][b]Read and clear[/b]: a notice stays new until you open it. Mark all read marks every notice as read, and Clear list empties the list for this save after asking you first.[/*]
[*][b]Kept with your save[/b]: each save has a separate list that keeps the newest 500 notices, and it comes back when you load that save again.[/*]
[/list]
Popups that ask you to choose something, such as events and the item pickup window with Pick All, are never changed.

Notifications adds nothing to your save and does not edit game files. The lists are kept in separate files next to the mod's settings. You can remove the mod at any time.

Works in English and Chinese.

[size=5]Known issues[/size]
[list]
[*]After a restart, a notice in the list can open its popup again only if the mod saved enough to rebuild that popup.[/*]
[*]A notice from another mod can only be opened until you quit the game.[/*]
[*]When some of a drone's cargo does not fit, that cargo stays in the drone as before, and the toast has no popup to open.[/*]
[*]The list starts empty for saves played before the mod was installed.[/*]
[/list]

[center][img]https://sldb.core-directive.com/img/mods-banner.jpg[/img]
[size=4][url=https://www.nexusmods.com/survivallog/mods/73]Get Mod Manager[/url] to set up all your mods in game  |  [url=https://sldb.core-directive.com/en/]Open the Survival Log wiki[/url] for items, recipes and maps[/size][/center]

[size=5]Requirements[/size]
[list]
[*]Survival Log 1.1.20719. A game update can break the mod until it's updated.[/*]
[*][url=https://www.nexusmods.com/survivallog/mods/12]BepInEx for Survival Log[/url]. Install it first.[/*]
[*]Exiled's Toolkit 0.19.0 or newer, which draws the toasts and applies changed settings at once. Notifications does not load without it.[/*]
[*]Optional: [url=https://www.nexusmods.com/survivallog/mods/73]Mod Manager[/url]. With it, you can change every setting in game on its Mods screen and see what each one does, without opening the config file.[/*]
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
You choose which popups and messages become toasts. Change it in game on the Mod Manager's Mods screen, where changes take effect at once, or edit [font=Courier New]BepInEx\config\com.dev.survivallog.notifications.cfg[/font] while the game is closed. The cfg file explains each setting in English and Chinese, and [url=https://github.com/AnExiledDev/SurvivalLog-Notifications/blob/main/CONFIG.md]CONFIG.md[/url] lists them all. Where the toasts appear and how long they stay are Exiled's Toolkit settings.

[size=5]Feedback[/size]
Found a bug or have an idea? Come say hi on the [url=https://discord.gg/trTsyxUFqg]Core Directive Discord[/url], or post in this page's Posts tab. For bugs, please include [font=Courier New]BepInEx\LogOutput.log[/font] and [font=Courier New]BepInEx\notifications.log[/font].
To reach me directly, message [b]exiled.dev[/b] on Discord. You can find me in the [url=https://discord.gg/MZGg4gQAUe]official Survival Log Discord[/url]. I'm also on X as [url=https://x.com/AnExiledDev]@AnExiledDev[/url].

[size=5]Source and license[/size]
Open source under MIT: [url=https://github.com/AnExiledDev/SurvivalLog-Notifications]github.com/AnExiledDev/SurvivalLog-Notifications[/url]

[line]
[size=5]简体中文[/size]

Survival Log 会为许多只需看一眼的事情弹出窗口、打断游戏：愿望满足、获得物品、熟练度提升、无人机卸货。它的简短提示（例如精力不足）一闪而过，很容易错过。Notifications 把这些都改为提示显示，并在手机上保存完整的列表，即使你没看到也不会丢失。

[size=5]主要功能[/size]
[list]
[*][b]弹窗变成提示[/b]：愿望满足、获得物品、熟练度提升和无人机送货都显示为提示，不再打断游戏。每一项都可以单独改回弹窗。尸潮战报默认仍以弹窗显示，开启 WaveReport 后才改为提示。[/*]
[*][b]点击打开[/b]：点击提示会打开它替代的弹窗，内容和原来一样。[/*]
[*][b]简短提示和解锁[/b]：游戏的简短提示、系统解锁和新幸存者解锁都改为提示显示，字更大，停留更久。也可以照常由游戏显示，同时在手机上留一份记录。[/*]
[*][b]篮子回来了[/b]：邻居或同行者的「篮子」带着回礼回来时显示提示。不装模组时，游戏只会发一条短信。[/*]
[*][b]重复合并[/b]：同一条提示连续出现多次时，只显示一条提示并带上次数（x2、x3 等），不会堆成一串。[/*]
[*][b]手机应用[/b]：手机上的「通知」应用列出当前存档的每条通知，最新的在前，按游戏日期分组，并显示时刻。可以筛选只看新通知、弹窗、消息或其他模组的通知。图标上显示新通知的数量，主屏幕上显示最新的三条。[/*]
[*][b]在列表里看详情[/b]：点击列表中的通知可以查看详情，例如获得了哪些物品、各多少，或者愿望带来了什么。如果通知来自弹窗，点「打开弹窗」可以再次显示那个弹窗。[/*]
[*][b]已读与清空[/b]：通知在你打开之前一直算作新通知。「全部已读」把所有通知标为已读，「清空列表」会先问你，然后清空本存档的列表。[/*]
[*][b]随存档保存[/b]：每个存档有自己的列表，保留最新的 500 条通知，再次读取该存档时会回来。[/*]
[/list]
需要你做选择的弹窗，例如事件和带有全部拾取按钮的拾取窗口，永远不会被改动。

Notifications 不会往存档里写入任何内容，也不会修改游戏文件。列表单独保存在模组设置旁边的文件里。你可以随时移除这个模组。

支持英文和中文。

[size=5]已知问题[/size]
[list]
[*]重启游戏后，只有模组保存了足够的内容来重建弹窗时，列表里的通知才能再次打开它。[/*]
[*]其他模组的通知只能在退出游戏之前打开。[/*]
[*]无人机的部分货物放不下时，会像以前一样留在无人机里，此时提示没有可打开的弹窗。[/*]
[*]安装模组之前玩过的存档，列表从空开始。[/*]
[/list]

[url=https://www.nexusmods.com/survivallog/mods/73]获取 Mod Manager[/url]，在游戏里设置所有模组。[url=https://sldb.core-directive.com/zh/]打开 Survival Log 维基[/url]，物品、配方、地图一查就有。

[size=5]前置要求[/size]
[list]
[*]Survival Log 1.1.20719。游戏更新后模组可能暂时失效，直到模组更新为止。[/*]
[*][url=https://www.nexusmods.com/survivallog/mods/12]BepInEx for Survival Log[/url]，请先安装。[/*]
[*]Exiled's Toolkit 0.19.0 或更新版本，用于显示通知，并让改动的设置立即生效。没有它，Notifications 无法加载。[/*]
[*]可选：[url=https://www.nexusmods.com/survivallog/mods/73]Mod Manager[/url]。装了它，可以在游戏里它的「模组」界面修改每项设置，并看到每项的说明，不用打开配置文件。[/*]
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
哪些弹窗和消息改为提示显示，由你决定。可以在游戏内 Mod Manager 的「模组」界面修改（改动立即生效），也可以在游戏关闭时编辑 [font=Courier New]BepInEx\config\com.dev.survivallog.notifications.cfg[/font]。配置文件中每项设置都有中文说明，完整说明见 [url=https://github.com/AnExiledDev/SurvivalLog-Notifications/blob/main/CONFIG.md]CONFIG.md[/url]（英文）。

[size=5]反馈[/size]
发现 bug 或有好点子？欢迎来 [url=https://discord.gg/trTsyxUFqg]Core Directive Discord[/url] 打个招呼，或在本页的「Posts」标签里发帖。报告 bug 时，请附上 [font=Courier New]BepInEx\LogOutput.log[/font] 和 [font=Courier New]BepInEx\notifications.log[/font]。
想直接联系我，可以在 Discord 上给 [b]exiled.dev[/b] 发消息，我在 [url=https://discord.gg/MZGg4gQAUe]Survival Log 官方 Discord[/url] 里。也可以在 X 上找我：[url=https://x.com/AnExiledDev]@AnExiledDev[/url]。

[size=5]源代码与许可[/size]
基于 MIT 许可开源：[url=https://github.com/AnExiledDev/SurvivalLog-Notifications]github.com/AnExiledDev/SurvivalLog-Notifications[/url]
