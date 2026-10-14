using System;
using System.Reflection;
using HarmonyLib;

namespace Notifications.Adapters;

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
