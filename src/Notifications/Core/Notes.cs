using System;
using System.Collections.Generic;
using System.Linq;

namespace Notifications.Core;

/// <summary>The Toolkit's toast kinds, mirrored so the core needs no Toolkit assembly.</summary>
public enum NoteKind
{
    Info,
    Success,
    Warning,
    Error,
}

/// <summary>What a notice says. Two notices with equal content are the same notice raised twice.</summary>
public sealed record Note(string Source, string Title, string? Body, NoteKind Kind);

/// <summary>The in-game day and hour a notice came in.</summary>
public readonly record struct GameStamp(int Day, int Hour);

/// <summary>One line of a received-items popup: an item and how many.</summary>
public readonly record struct ItemStack(int ConfigId, int Count);

/// <summary>
/// One toast trigger as the Toolkit reports it, with the id as a plain number. A merge carries the id of the toast it
/// joined.
/// </summary>
public sealed record ToastNote(long Id, Note Content, DateTime RaisedAtUtc, bool HasClickAction, bool IsMerge)
{
    /// <summary>The picture the toast showed, as a path from the game's WebUI folder, or null.</summary>
    public string? Icon { get; init; }
}

/// <summary>
/// What it takes to open a game popup again with the same data it first had. Kept with the history entry, and saved
/// with it, so a notice can be clicked after a restart.
/// </summary>
public abstract record Reopen
{
    private Reopen()
    {
    }

    /// <summary>A wish came true: the wish popup.</summary>
    public sealed record Wish(int ItemConfigId, int FaithValue, int SurvivalValue, int BuffId, int WishPoolId) : Reopen;

    /// <summary>The received-items popup, with every argument the game passed.</summary>
    public sealed record Items(IReadOnlyList<ItemStack> Stacks, IReadOnlyList<int>? Kinds, int BumperCount, int SuperCount, string? Presentation, string? Desc) : Reopen
    {
        public bool Equals(Items? other) =>
            other is not null && Stacks.SequenceEqual(other.Stacks) && SameKinds(Kinds, other.Kinds) && BumperCount == other.BumperCount &&
            SuperCount == other.SuperCount && Presentation == other.Presentation && Desc == other.Desc;

        public override int GetHashCode() => HashCode.Combine(Stacks.Count, BumperCount, SuperCount, Presentation, Desc);

        private static bool SameKinds(IReadOnlyList<int>? left, IReadOnlyList<int>? right) =>
            left == null || right == null ? left == right : left.SequenceEqual(right);
    }

    /// <summary>The zombie wave report.</summary>
    public sealed record Wave(int WaveIndex, int KillCount, int DeviceDamage, int StructureDamage, string? RewardText) : Reopen;

    /// <summary>A skill level-up: the level-up info's properties by name, as invariant text.</summary>
    public sealed record Skill(IReadOnlyDictionary<string, string> Fields) : Reopen
    {
        public bool Equals(Skill? other) =>
            other is not null && Fields.Count == other.Fields.Count &&
            Fields.All(field => other.Fields.TryGetValue(field.Key, out var value) && value == field.Value);

        public override int GetHashCode() => Fields.Count;
    }
}
