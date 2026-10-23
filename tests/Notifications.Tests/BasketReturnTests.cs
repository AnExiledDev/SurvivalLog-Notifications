using Notifications.Core;
using Xunit;

namespace Notifications.Tests;

public class BasketReturnTests
{
    [Theory]
    [InlineData(1, 0, true)]
    [InlineData(2, 1, true)]
    [InlineData(1, 1, false)]
    [InlineData(0, 0, false)]
    public void ABasketCameBackOnlyWhenTheSettleUsedUpAScheduledReturn(int before, int after, bool cameBack) =>
        Assert.Equal(cameBack, BasketReturn.CameBack(before, after));

    [Fact]
    public void TheBasketToastSaysItIsBackInTheGamesLanguage()
    {
        Assert.Equal(new Note("Game", "The basket is back", "Her return gifts are in the basket.", NoteKind.Info), NoteTexts.BasketBack(false));
        Assert.Equal("篮子回来了", NoteTexts.BasketBack(true).Title);
    }

    [Fact]
    public void TheLogLineNamesBasketReturnsOnlyWhenTheSwitchIsOn()
    {
        var on = new Switches(false, false, false, false, false, false, false, false, false, BasketBack: true);

        Assert.EndsWith("; basket returns", on.Describe());
        Assert.DoesNotContain("basket", (on with { BasketBack = false }).Describe());
    }
}
