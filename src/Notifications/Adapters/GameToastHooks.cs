using System;
using GameCore.HotUpdate.ReduxUI;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Notifications.Core;

namespace Notifications.Adapters;

/// <summary>
/// The game's own small toasts: short texts such as "not enough stamina", system unlocks and survivor unlocks. Each
/// group either becomes a Toolkit toast (a merge of identical repeats shows one toast with a count), or stays the
/// game's and gets a copy in the history. An unlock draws its toast through the short-text call, so while an unlock
/// the game still shows is under way, that inner text is not taken a second time. Static because a Harmony patch has
/// nowhere else to look.
/// </summary>
internal static class GameToastHooks
{
    private static NoticeDesk? _desk;
    private static int _unlocksShowing;
    private static SwitchedHook? _popText;
    private static SwitchedHook? _systemUnlock;
    private static SwitchedHook? _characterUnlock;

    public static void Apply(Harmony harmony, NoticeDesk desk, Action<string> log)
    {
        _desk = desk;

        // 0x27f32f0
        _popText = new SwitchedHook(() => HookPatch.Apply(harmony, AccessTools.Method(typeof(Ac_PopText_AddPopText), nameof(Ac_PopText_AddPopText.SendAction)), typeof(GameToastHooks),
            nameof(BeforePopText), null, "the game's short texts go to toasts and the history", "they stay as the game draws them and are not in the history", log));

        // 0x2822f90
        _systemUnlock = new SwitchedHook(() => HookPatch.Apply(harmony, AccessTools.Method(typeof(LogicAdapter), nameof(LogicAdapter.OnSystemUnlockToast)), typeof(GameToastHooks),
            nameof(BeforeSystemUnlock), nameof(AfterUnlock), "system unlocks go to toasts and the history", "they stay as the game draws them and are not in the history", log));

        // 0x2812e70
        _characterUnlock = new SwitchedHook(() => HookPatch.Apply(harmony, AccessTools.Method(typeof(LogicAdapter), nameof(LogicAdapter.OnCharacterUnlockToast)), typeof(GameToastHooks),
            nameof(BeforeCharacterUnlock), nameof(AfterUnlock), "survivor unlocks go to toasts and the history", "they stay as the game draws them and are not in the history", log));
    }

    /// <summary>Called on the main thread at start and after each change of a switch.</summary>
    public static void Use(Switches switches)
    {
        _popText?.Use(switches.PopText);
        _systemUnlock?.Use(switches.SystemUnlock);
        _characterUnlock?.Use(switches.CharacterUnlock);
    }

    private static bool BeforePopText(string Text)
    {
        if (_unlocksShowing > 0 || _desk is not { } desk) return true;

        return desk.IsGameShowing("short text", desk.ForGameToast(desk.Switches.PopText), () =>
            NoteTexts.PopText(GameState.IsChinese(), Text) is { } note ? (note, null) : null);
    }

    private static bool BeforeSystemUnlock(int funcSwitchId, out bool __state)
    {
        __state = false;

        if (_desk is not { } desk) return true;

        var handling = desk.ForGameToast(desk.Switches.SystemUnlock);
        var isGameShowing = desk.IsGameShowing("system unlock", handling, () =>
        {
            var words = new GameWords();
            var (name, tips) = words.SystemLock(funcSwitchId);

            return (NoteTexts.SystemUnlock(words, name, tips), null);
        });

        return Showing(handling, isGameShowing, out __state);
    }

    /// <summary>The game shows a survivor unlock only over its main screen (CoreUI1); elsewhere it shows nothing, and so does this mod.</summary>
    private static bool BeforeCharacterUnlock(int playerSelectId, out bool __state)
    {
        __state = false;

        if (_desk is not { } desk || !IsMainScreenUp(desk)) return true;

        var handling = desk.ForGameToast(desk.Switches.CharacterUnlock);
        var isGameShowing = desk.IsGameShowing("survivor unlock", handling, () =>
        {
            var words = new GameWords();

            return (NoteTexts.CharacterUnlock(words, words.SurvivorName(playerSelectId)), null);
        });

        return Showing(handling, isGameShowing, out __state);
    }

    /// <summary>The same check the game's OnCharacterUnlockToast makes. A check that fails counts as not up, so the game decides.</summary>
    private static bool IsMainScreenUp(NoticeDesk desk)
    {
        try
        {
            return GameSingleton.Existing<ReduxUISystem>()?.IsUIActive(Il2CppType.Of<WebUI_CoreUI1>()) == true;
        }
        catch (Exception ex)
        {
            desk.Fail("checking the main screen for a survivor unlock", ex);

            return false;
        }
    }

    /// <summary>Runs whether or not the game's unlock ran, so the count always comes back down.</summary>
    private static void AfterUnlock(bool __state)
    {
        if (__state) _unlocksShowing--;
    }

    /// <summary>
    /// An unlock the game still shows, after this mod took its copy, keeps its inner short text to itself. A group
    /// that is switched off is the game's alone, so its inner text is a short text like any other.
    /// </summary>
    private static bool Showing(GameToastHandling handling, bool isGameShowing, out bool isCounted)
    {
        isCounted = isGameShowing && handling != GameToastHandling.Leave;

        if (isCounted) _unlocksShowing++;

        return isGameShowing;
    }
}
