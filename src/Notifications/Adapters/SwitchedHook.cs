using System;

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
