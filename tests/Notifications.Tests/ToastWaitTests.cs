using Notifications.Core;
using Xunit;

namespace Notifications.Tests;

public class ToastWaitTests
{
    private readonly ToastWait _wait = new();

    [Fact]
    public void AReportedToastTakesThePopupItWasRaisedFor()
    {
        _wait.Add(1, Make.Wish(), Make.T0);

        Assert.Equal(Make.Wish(), _wait.Take(1));
        Assert.Null(_wait.Take(1));
    }

    [Fact]
    public void RepeatsMergedIntoOneToastTakeTheirPopupsInTheOrderTheyWereRaised()
    {
        _wait.Add(1, Make.Wish(1), Make.T0);
        _wait.Add(1, Make.Wish(2), Make.T0);

        Assert.Equal(Make.Wish(1), _wait.Take(1));
        Assert.Equal(Make.Wish(2), _wait.Take(1));
    }

    [Fact]
    public void APopupThatCameAfterItsToastGoesWithTheToastsLastReport()
    {
        _wait.Add(1, null, Make.T0);

        Assert.True(_wait.Attach(1, Make.Wish()));
        Assert.Equal(Make.Wish(), _wait.Take(1));
    }

    [Fact]
    public void ALatePopupForAToastNotWaitingIsLeftToTheHistory() => Assert.False(_wait.Attach(1, Make.Wish()));

    [Fact]
    public void AToastTheToolkitNeverReportedIsForgottenOnceTheLimitHasPassed()
    {
        _wait.Add(1, Make.Wish(1), Make.T0);
        _wait.Add(2, Make.Wish(2), Make.T0 + ToastWait.Limit + TimeSpan.FromSeconds(1));

        Assert.Null(_wait.Take(1));
        Assert.False(_wait.Attach(1, Make.Wish()));
        Assert.Equal(Make.Wish(2), _wait.Take(2));
    }

    [Fact]
    public void AToastStillWithinTheLimitKeepsWaiting()
    {
        _wait.Add(1, Make.Wish(1), Make.T0);
        _wait.Add(2, Make.Wish(2), Make.T0 + ToastWait.Limit);

        Assert.Equal(Make.Wish(1), _wait.Take(1));
    }
}
