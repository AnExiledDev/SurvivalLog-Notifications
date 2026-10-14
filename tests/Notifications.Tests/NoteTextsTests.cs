using Notifications.Core;
using Xunit;

namespace Notifications.Tests;

public class NoteTextsTests
{
    [Fact]
    public void AToastReadsInTheGamesOwnWordsWhenItHasThem()
    {
        var words = new FakeWords(isChinese: true);

        words.Constants["UI_AddItemPop_0"] = "获得物品";
        words.Items[10] = "木头";

        var note = NoteTexts.Items(words, Make.Items(null, (10, 3)));

        Assert.Equal(new Note("游戏", "获得物品", "木头 x3", NoteKind.Success), note);
    }

    [Fact]
    public void WithoutTheGamesTextAToastFallsBackToPlainWords()
    {
        var words = new FakeWords();

        words.Items[10] = "Wood";

        Assert.Equal(new Note("Game", "Items received", "Wood x3", NoteKind.Success), NoteTexts.Items(words, Make.Items(null, (10, 3))));
    }

    [Fact]
    public void AnItemTheGameCannotNameIsShownByItsNumber() =>
        Assert.Equal("#77 x1", NoteTexts.ItemLines(new FakeWords(), Make.Cargo((77, 1))));

    [Fact]
    public void ALongItemListShowsTheFirstSixAndHowManyMore()
    {
        var cargo = Enumerable.Range(1, NoteTexts.MostItemsListed + 2).Select(id => (id, 1)).ToArray();

        Assert.EndsWith("#6 x1, +2 more", NoteTexts.ItemLines(new FakeWords(), Make.Cargo(cargo)));
        Assert.EndsWith("另有 2 种", NoteTexts.ItemLines(new FakeWords(isChinese: true), Make.Cargo(cargo)));
    }

    [Fact]
    public void AGameToastLosesItsRichTextTagsAndExtraSpaces() =>
        Assert.Equal("Not enough stamina", NoteTexts.PopText(false, "  <color=#ff0000>Not enough\n stamina</color> ")?.Title);

    [Fact]
    public void AGameToastWithNothingLeftToShowIsNoNotice() => Assert.Null(NoteTexts.PopText(false, "<b> </b>"));

    [Fact]
    public void TheSameGameToastTwiceIsTheSameNoticeSoTheToolkitMergesIt() =>
        Assert.Equal(NoteTexts.PopText(true, "体力不足"), NoteTexts.PopText(true, "体力不足"));

    [Fact]
    public void AFulfilledWishNamesTheFoodItsRewardsAndItsBuff()
    {
        var words = new FakeWords();

        words.Items[5] = "Cake";

        var note = NoteTexts.Wish(words, new Reopen.Wish(5, 3, 2, 8, 1), "Content");

        Assert.Equal("Wish fulfilled", note.Title);
        Assert.Equal("Finally had the Cake you longed for, Faith +3, Survival points +2, Content", note.Body);
    }

    [Fact]
    public void AWaveReportNamesTheWaveAndItsLosses()
    {
        var note = NoteTexts.Wave(new FakeWords(), new Reopen.Wave(4, 30, 2, 1, ""));

        Assert.Equal("Wave 4 repelled", note.Title);
        Assert.Equal("Kills 30, Device wear 2, Door and window wear 1", note.Body);
    }

    [Fact]
    public void ASkillLevelUpNamesTheSkillItsLevelAndWhatItUnlocks()
    {
        var skill = new Reopen.Skill(new Dictionary<string, string> { ["SystemName"] = "Cooking", ["NewLevel"] = "3", ["UnlockedAbility"] = "Stew\nSoup" });

        var note = NoteTexts.Skill(new FakeWords(), skill);

        Assert.Equal("Skill up Cooking Lv.3", note.Title);
        Assert.Equal("Stew, Soup", note.Body);
    }

    [Fact]
    public void ADroneDeliveryListsItsCargo()
    {
        var words = new FakeWords(isChinese: true);

        words.Items[1] = "木头";
        words.Items[2] = "石头";

        Assert.Equal(new Note("游戏", "无人机送货到了", "木头 x4，石头 x1", NoteKind.Info), NoteTexts.Drone(words, Make.Cargo((1, 4), (2, 1))));
    }

    [Fact]
    public void UnlocksNameWhatWasUnlocked()
    {
        var words = new FakeWords();

        Assert.Equal("System unlocked", NoteTexts.SystemUnlock(words, "Workbench", "Craft tools").Title);
        Assert.Equal("Workbench, Craft tools", NoteTexts.SystemUnlock(words, "Workbench", "Craft tools").Body);
        Assert.StartsWith("Ann, ", NoteTexts.CharacterUnlock(words, "Ann").Body);
    }

    [Fact]
    public void FillingGameTextNeverThrowsOnAStrayBrace() => Assert.Equal("a {x} 5 }", NoteTexts.Fill("a {x} {0} }", "5"));
}
