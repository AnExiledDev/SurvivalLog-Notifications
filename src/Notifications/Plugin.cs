using System.IO;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using ExiledToolkit;
using HarmonyLib;
using Notifications.Adapters;
using Notifications.Core;

namespace Notifications;

[BepInPlugin(Guid, "Notifications", Version)]
[BepInDependency("com.dev.survivallog.exiledtoolkit")]
public sealed class Plugin : BasePlugin
{
    private const string Guid = "com.dev.survivallog.notifications";
    private const string Version = "0.1.0";

    public override void Load()
    {
        var switches = ReadSwitches();
        var log = new NoteLog(Log, Path.Combine(Paths.BepInExRootPath, "notifications.log"));
        var ledger = new Ledger(new HistoryFiles(Path.Combine(Paths.ConfigPath, "notifications")), log.Write);
        var bridge = new ToastBridge(ledger, log.Write);
        var closer = new PopupCloser(log.Write);
        var desk = new NoticeDesk(switches, ledger, bridge, closer, log.Write);
        var harmony = new Harmony(Guid);

        log.Write($"===== Notifications {Version} started: {switches.Describe()}");

        Toasts.Raised += bridge.OnRaised;
        PageMessages.Apply(harmony, log.Write);
        PopupHooks.Apply(harmony, desk, log.Write);

        if (switches.DroneDelivery) DroneHooks.Apply(harmony, desk, log.Write);

        GameToastHooks.Apply(harmony, desk, log.Write);

        NoteTicker.Log = log.Write;
        NoteTicker.Ledger = ledger;
        NoteTicker.Phone = new PhoneApp(ledger, desk, log.Write);
        NoteTicker.Closer = closer;

        AddComponent<NoteTicker>();
    }

    private Switches ReadSwitches() => new(
        WishFulfilled: Popup("WishFulfilled",
            "Show a toast instead of the popup when a wish comes true. Click the toast, or the entry in the phone's Notifications app, to open the popup.\n" +
            "愿望达成时显示提示，不再弹出窗口。点击提示或手机「通知」里的记录可打开原窗口。"),
        ItemsReceived: Popup("ItemsReceived",
            "Show a toast instead of the received-items popup (bumper harvests, first-time dishes, rewards). Click it to open the popup. The small pickup pops that fade by themselves stay as they are.\n" +
            "获得物品时显示提示，不再弹出窗口（丰收、初次烹成、奖励等）。点击提示可打开原窗口。拾取物品时自动消失的小提示保持不变。"),
        WaveReport: Popup("WaveReport",
            "Show a toast instead of the zombie wave report. Click it to open the report.\n" +
            "尸潮战报改为提示显示，不再弹出窗口。点击提示可打开战报。"),
        SkillLevelUp: Popup("SkillLevelUp",
            "Show a toast instead of the skill level-up popup. Click it to open the popup.\n" +
            "熟练度提升时显示提示，不再弹出窗口。点击提示可打开原窗口。"),
        DroneDelivery: Popup("DroneDelivery",
            "When a drone comes home, show a \"Drone delivery arrived\" toast instead of the unload popup. Click it to open the popup. Cargo that does not fit stays in the drone, as before, and then there is no popup to open.\n" +
            "无人机返回时显示「无人机送货到了」提示，不再弹出卸货窗口。点击提示可打开原窗口。放不下的货物仍留在无人机里，此时没有窗口可打开。"),
        PopText: GameToast("PopText",
            "Show the game's short messages (such as not enough stamina) as toasts. Identical messages in a row merge into one toast with a count. All of them go into the phone's Notifications app.\n" +
            "把游戏的简短提示（例如体力不足）改为本模组的提示显示。连续出现的相同提示会合并为一条并显示次数。所有提示都会记入手机「通知」。"),
        SystemUnlock: GameToast("SystemUnlock",
            "Show system unlocks as toasts, and keep them in the phone's Notifications app.\n" +
            "系统解锁改为提示显示，并记入手机「通知」。"),
        CharacterUnlock: GameToast("CharacterUnlock",
            "Show new survivor unlocks as toasts, and keep them in the phone's Notifications app.\n" +
            "新幸存者解锁改为提示显示，并记入手机「通知」。"),
        KeepGameToastLook: Config.Bind("GameToasts", "KeepGameToastLook", false,
            "Let the game draw its own short messages and unlocks as before, and only copy them into the phone's Notifications app. Off: they show as toasts, which stay on screen as long as Exiled's Toolkit's Seconds setting says (7 seconds by default).\n" +
            "游戏的简短提示和解锁仍由游戏自己显示，只把它们记入手机「通知」。关闭时改为提示显示，停留时间由 Exiled's Toolkit 的 Seconds 设置决定（默认 7 秒）。").Value);

    private bool Popup(string key, string description) => Config.Bind("Popups", key, true, description).Value;

    private bool GameToast(string key, string description) => Config.Bind("GameToasts", key, true, description).Value;
}
