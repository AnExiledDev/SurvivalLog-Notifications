using System;
using System.Reflection;
using HarmonyLib;

namespace Notifications.Adapters;

/// <summary>
/// One switch's hook, put in the first time the switch is on, so a switch never turned on leaves the game's method
/// untouched. Once in, it stays: each hook reads its switch on every call and leaves an off switch's notice to the game.
/// </summary>
internal sealed class SwitchedHook
{
    private readonly Action _install;
    private bool _isTried;

    public SwitchedHook(Action install) => _install = install;

    /// <summary>A hook that failed to go in is not tried again; its failure was logged once.</summary>
    public void Use(bool isOn)
    {
        if (!isOn || _isTried) return;

        _isTried = true;
        _install();
    }
}

/// <summary>Puts one Harmony patch on; a game method that cannot be patched costs only the conversion it serves.</summary>
internal static class HookPatch
{
    public static void Apply(Harmony harmony, MethodBase? target, Type hooks, string? prefix, string? postfix, string done, string consequence, Action<string> log)
    {
        try
        {
            if (target == null) throw new MissingMethodException("the game method was not found");

            harmony.Patch(
                target,
                prefix: prefix == null ? null : new HarmonyMethod(hooks, prefix),
                postfix: postfix == null ? null : new HarmonyMethod(hooks, postfix));
            log(done);
        }
        catch (Exception ex)
        {
            log($"could not hook {target?.DeclaringType?.Name}.{target?.Name}, {consequence}: {ex.Message}");
        }
    }
}
