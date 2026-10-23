namespace Notifications.Core;

/// <summary>The rule for the neighbour's or companion's basket coming back.</summary>
public static class BasketReturn
{
    /// <summary>
    /// Whether a settle brought a basket back: the game keeps one scheduled return per basket sent and drops it once the
    /// gifts are in, so fewer scheduled returns after the settle than before means one came back. A settle that ran
    /// early, or found nothing due, leaves the count as it was.
    /// </summary>
    public static bool CameBack(int scheduledBefore, int scheduledAfter) => scheduledBefore > 0 && scheduledAfter < scheduledBefore;
}
