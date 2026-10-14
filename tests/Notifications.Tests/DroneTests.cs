using Notifications.Core;
using Xunit;

namespace Notifications.Tests;

public class DroneCargoTests
{
    [Theory]
    [InlineData(false, true, false, "{\"1\":2}", true)]
    [InlineData(true, true, false, "{\"1\":2}", false)]
    [InlineData(false, false, false, "{\"1\":2}", false)]
    [InlineData(false, true, true, "{\"1\":2}", false)]
    [InlineData(false, true, false, "", false)]
    public void ADroneHandsOverCargoOnlyFromAnOpenTradePostOnATripThatIsNotCharity(bool isTradeLocked, bool isTripBusy, bool isCharity, string cargo, bool handsOver) =>
        Assert.Equal(handsOver, DroneCargo.HandsOver(isTradeLocked, isTripBusy, isCharity, cargo));

    [Fact]
    public void TheCargoIsReadInItsOrderWithNothingButPositiveCountsOfNumberedItems() =>
        Assert.Equal(Make.Cargo((5, 2), (7, 1)), DroneCargo.Parse("{\"5\":2,\"x\":3,\"9\":0,\"7\":1,\"-2\":4}"));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("[1,2]")]
    public void CargoTextOfAnyOtherShapeIsNoCargo(string? cargo) => Assert.Empty(DroneCargo.Parse(cargo));

    [Fact]
    public void APopupIsTheDronesWhenEveryLineIsCargoItCarried()
    {
        var cargo = Make.Cargo((5, 2), (7, 1));

        Assert.True(DroneCargo.Covers(cargo, Make.Cargo((5, 2))));
        Assert.True(DroneCargo.Covers(cargo, Make.Cargo((5, 1), (7, 1))));
        Assert.False(DroneCargo.Covers(cargo, Make.Cargo((5, 3))));
        Assert.False(DroneCargo.Covers(cargo, Make.Cargo((8, 1))));
        Assert.False(DroneCargo.Covers(cargo, Make.Cargo()));
    }
}

public class DroneAwaitTests
{
    private const string Unload = "Unloaded from the drone";

    [Fact]
    public void TheUnloadPopupOfAWaitingDroneIsClaimedAndKeptForItsToast()
    {
        var drones = new DroneAwait();
        var arrival = drones.Expect(Make.Cargo((5, 2)), 0);
        var popup = Make.Items(Unload, (5, 2));

        Assert.Same(arrival, drones.Claim(popup, Unload, 1));
        Assert.Equal(popup, arrival.Popup);
    }

    [Fact]
    public void ADroneClaimsOnlyOnePopup()
    {
        var drones = new DroneAwait();

        drones.Expect(Make.Cargo((5, 2)), 0);
        drones.Claim(Make.Items(Unload, (5, 2)), Unload, 1);

        Assert.Null(drones.Claim(Make.Items(Unload, (5, 2)), Unload, 2));
    }

    [Fact]
    public void AReceivedItemsPopupWithoutTheUnloadLineIsNotTheDrones()
    {
        var drones = new DroneAwait();

        drones.Expect(Make.Cargo((5, 2)), 0);

        Assert.Null(drones.Claim(Make.Items("Bumper harvest", (5, 2)), Unload, 1));
        Assert.Null(drones.Claim(Make.Items(null, (5, 2)), Unload, 1));
        Assert.Null(drones.Claim(Make.Items(Unload, (5, 2)), null, 1));
    }

    [Fact]
    public void ThePopupGoesToTheDroneThatCarriedItsItems()
    {
        var drones = new DroneAwait();
        var wood = drones.Expect(Make.Cargo((1, 4)), 0);
        var stone = drones.Expect(Make.Cargo((2, 3)), 0);

        Assert.Same(stone, drones.Claim(Make.Items(Unload, (2, 3)), Unload, 1));
        Assert.Same(wood, drones.Claim(Make.Items(Unload, (1, 4)), Unload, 1));
    }

    [Fact]
    public void ADroneWhoseCargoIsExactlyThePopupWinsOverAnOlderOneThatCarriedMore()
    {
        var drones = new DroneAwait();
        var bigger = drones.Expect(Make.Cargo((1, 4), (2, 3)), 0);
        var exact = drones.Expect(Make.Cargo((2, 3)), 0);

        Assert.Same(exact, drones.Claim(Make.Items(Unload, (2, 3)), Unload, 1));
        Assert.Same(bigger, drones.Claim(Make.Items(Unload, (2, 3), (1, 4)), Unload, 1));
    }

    [Fact]
    public void WithNoExactMatchTheDroneThatCameHomeFirstAmongThoseThatCarriedItClaimsThePopup()
    {
        var drones = new DroneAwait();
        var first = drones.Expect(Make.Cargo((1, 4), (2, 3)), 0);
        var second = drones.Expect(Make.Cargo((2, 5)), 0);

        Assert.Same(first, drones.Claim(Make.Items(Unload, (2, 2)), Unload, 1));
        Assert.Same(second, drones.Claim(Make.Items(Unload, (2, 2)), Unload, 1));
    }

    [Fact]
    public void ADroneStopsWaitingAfterTheWaitRunsOut()
    {
        var drones = new DroneAwait();

        drones.Expect(Make.Cargo((5, 2)), 0);

        Assert.Null(drones.Claim(Make.Items(Unload, (5, 2)), Unload, DroneAwait.WaitSeconds + 1));
    }

    [Fact]
    public void ADroneGivenUpHandsBackThePopupItClaimedSoItIsNotLost()
    {
        var drones = new DroneAwait();
        var arrival = drones.Expect(Make.Cargo((5, 2)), 0);
        var popup = Make.Items(Unload, (5, 2));

        drones.Claim(popup, Unload, 1);

        Assert.Equal(popup, drones.GiveUp(arrival));
    }

    [Fact]
    public void ADroneGivenUpBeforeItsPopupCameNoLongerClaimsIt()
    {
        var drones = new DroneAwait();
        var arrival = drones.Expect(Make.Cargo((5, 2)), 0);

        Assert.Null(drones.GiveUp(arrival));
        Assert.Null(drones.Claim(Make.Items(Unload, (5, 2)), Unload, 1));
    }
}
