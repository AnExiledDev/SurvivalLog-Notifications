using Notifications.Core;
using Xunit;

namespace Notifications.Tests;

/// <summary>The phone app as the player sees it: notifications.js run on a fake phone page.</summary>
public class PhoneAppPageTests
{
    private static Note ModNote(string title, string? body = null) => new("Storage Hub", title, body, NoteKind.Warning);

    private static ToastNote ModToast(long id, string title, bool isMerge = false) => new(id, ModNote(title, "Rest first"), Make.T0, false, isMerge);

    [Fact]
    public void ThePhonesDockGetsANotificationsAppNextToTheGamesApps()
    {
        var page = PageScript.WithPhone();

        page.Install(null);

        Assert.Equal(3, page.Count(".app-dock .app"));
        Assert.Equal("Notifications", page.TextAt(".app-dock .is-slnotify .app-label"));
    }

    [Fact]
    public void TheAppIsNamedInChineseForAChinesePlayer()
    {
        var page = PageScript.WithPhone();

        page.Install(null, isChinese: true);
        page.Click(".is-slnotify");

        Assert.Equal("通知", page.TextAt(".is-slnotify .app-label"));
        Assert.Equal("暂无通知", page.TextAt(".slnotify-view .sn-et"));
    }

    [Fact]
    public void SendingTheScriptAgainAddsNoSecondApp()
    {
        var page = PageScript.WithPhone();

        page.Install(null);
        page.Install(null);
        page.Run("runTimers();");

        Assert.Equal(1, page.Count(".is-slnotify"));
        Assert.Equal(1, page.Count(".slnotify-view"));
        Assert.Equal(1, page.Count(".app-grid .sn-peek"));
    }

    [Fact]
    public void TheListShowsEveryNoticeNewestFirstUnderItsDayWithItsHourAndCount()
    {
        var page = PageScript.WithPhone("第{0}天");
        var history = new History();

        history.Record(Make.Toast(1, "Wish fulfilled"), new GameStamp(3, 7), Make.Wish());
        history.Record(ModToast(2, "Not enough stamina"), new GameStamp(12, 18), null);
        history.Record(ModToast(2, "Not enough stamina", isMerge: true), new GameStamp(12, 18), null);
        history.Record(ModToast(2, "Not enough stamina", isMerge: true), new GameStamp(12, 18), null);

        page.Install(history, new FakeWords(), 12);
        page.Click(".is-slnotify");

        Assert.Equal(new[] { "Not enough stamina", "Wish fulfilled" }, page.Texts(".slnotify-row .sn-title"));
        Assert.Equal(new[] { "18:00", "07:00" }, page.Texts(".slnotify-row .sn-time"));
        Assert.Equal(new[] { "x3" }, page.Texts(".slnotify-row .sn-count"));
        Assert.Equal(new[] { "TODAY 第12天", "第3天" }, page.Texts(".sn-divider"));
        Assert.Equal("第12天", page.TextAt(".slnotify-view .sn-day"));
    }

    [Fact]
    public void AnotherModsNoticeShowsWhichModSentItAndAGameNoticeDoesNot()
    {
        var page = PageScript.WithPhone();
        var history = new History();

        history.Record(Make.Toast(1, "Shelter upgraded"), null, null);
        history.Record(ModToast(2, "Not enough stamina"), null, null);
        page.Install(history);
        page.Click(".is-slnotify");

        Assert.Equal(new[] { "Storage Hub Rest first" }, page.Texts(".slnotify-row .sn-body"));
        Assert.Equal(new[] { "Storage Hub" }, page.Texts(".slnotify-row .sn-src"));
    }

    [Fact]
    public void ReceivedItemsShowAsChipsWithPicturesThreeUntilTheRowIsOpened()
    {
        var page = PageScript.WithPhone();
        var history = new History();
        var words = new FakeWords();

        words.Items[10] = "Wood";
        words.Icons[10] = "Res/Item/wood.png";
        history.Record(Make.Toast(1, "Received"), null, Make.Items(null, (10, 4), (11, 1), (12, 2), (13, 7), (14, 1)));
        page.Install(history, words, null);
        page.Click(".is-slnotify");

        Assert.Equal(new[] { "Wood x4", "#11 x1", "#12 x2", "+2 more" }, page.Texts(".slnotify-row .sn-item"));
        Assert.Equal("https://ui.local/Res/Item/wood.png", page.Text("phone.document.querySelector('.sn-item img').getAttribute('src')"));
        Assert.Equal("https://ui.local/Res/Item/wood.png", page.Text("phone.document.querySelector('.slnotify-row .sn-tile img').getAttribute('src')"));

        page.ClickRow(history.Entries[0].Seq);

        Assert.Equal(5, page.Count(".slnotify-row .sn-item"));
        Assert.False(page.Has(".sn-more"));
    }

    [Fact]
    public void AWishShowsItsFaithAndSurvivalGainsAsStats()
    {
        var page = PageScript.WithPhone();
        var history = new History();

        history.Record(Make.Toast(1, "Wish fulfilled"), null, new Reopen.Wish(5, 3, 2, 0, 1));
        page.Install(history);
        page.Click(".is-slnotify");

        Assert.Equal(new[] { "Faith +3", "Survival points +2" }, page.Texts(".slnotify-row .sn-stat.sn-up"));
    }

    [Fact]
    public void AnEmptyHistorySaysSoAndOffersNothingToClear()
    {
        var page = PageScript.WithPhone();

        page.Install(new History());
        page.Click(".is-slnotify");

        Assert.Equal("No notices yet", page.TextAt(".slnotify-view .sn-et"));
        Assert.True(page.Has(".sn-foot.slnotify-hidden"));
        Assert.True(page.Has(".sn-filters.slnotify-hidden"));
    }

    [Fact]
    public void TheIconCountsUnreadNoticesAndOpeningTheListReadsNothing()
    {
        var page = PageScript.WithPhone();
        var history = new History();

        history.Record(Make.Toast(1, "a"), null, null);
        history.Record(Make.Toast(2, "b"), null, null);
        page.Install(history);

        Assert.Equal("2", page.TextAt(".is-slnotify .unread-dot"));
        Assert.False(page.Has(".is-slnotify .unread-dot.slnotify-hidden"));

        page.Click(".is-slnotify");

        Assert.True(page.Has(".slnotify-view.is-active"));
        Assert.Equal("2", page.TextAt(".is-slnotify .unread-dot"));
        Assert.Equal("2 new", page.TextAt(".slnotify-view .sn-new"));
        Assert.Empty(page.Posted());
    }

    [Fact]
    public void NothingUnreadShowsNoBadge()
    {
        var page = PageScript.WithPhone();
        var history = new History();

        history.Record(Make.Toast(1, "a"), null, null);
        history.MarkSeen(history.LatestMark);
        page.Install(history);

        Assert.True(page.Has(".is-slnotify .unread-dot.slnotify-hidden"));
        Assert.True(page.Has(".sn-readall.sn-off"));
    }

    [Fact]
    public void OpeningAnUnreadRowExpandsItInPlaceAndReadsIt()
    {
        var page = PageScript.WithPhone();
        var history = new History();

        history.Record(Make.Toast(1, "a"), null, null);
        history.Record(Make.Toast(2, "b"), null, null);
        page.Install(history);
        page.Click(".is-slnotify");
        page.ClickRow(history.Entries[1].Seq);

        Assert.Equal(new[] { "read|" + history.Entries[1].Seq }, page.Posted());
        Assert.Equal(new[] { "a" }, page.Texts(".slnotify-row.is-open .sn-title"));
        Assert.Equal(new[] { "b" }, page.Texts(".slnotify-row.is-unread .sn-title"));
        Assert.Equal("1", page.TextAt(".is-slnotify .unread-dot"));
        Assert.True(page.Has(".slnotify-view.is-active"));

        page.ClickRow(history.Entries[1].Seq);

        Assert.False(page.Has(".slnotify-row.is-open"));
        Assert.Single(page.Posted());
    }

    [Fact]
    public void AnOpenedRowWithAPopupOffersItAndOpeningItClosesThePhoneSoThePopupIsNotUnderIt()
    {
        var page = PageScript.WithPhone();
        var history = new History();

        history.Record(Make.Toast(1, "Wish fulfilled"), null, Make.Wish());
        page.Install(history);
        page.Click(".is-slnotify");
        page.ClickRow(history.Entries[0].Seq);

        Assert.Equal("Open popup", page.TextAt(".slnotify-row.is-open .sn-open"));

        page.Click(".slnotify-row.is-open .sn-open");

        Assert.Equal("open|" + history.Entries[0].Seq, page.Posted()[^1]);
        Assert.False(page.Has(".slnotify-view.is-active"));
        Assert.Equal(1, page.Number("phone.document.closedCount"));
    }

    [Fact]
    public void AnOpenedToastThatCanBeClickedOffersAPlainOpen()
    {
        var page = PageScript.WithPhone();
        var history = new History();

        history.Record(Make.Toast(1, "Order ready", hasClickAction: true), null, null);
        page.Install(history);
        page.Click(".is-slnotify");
        page.ClickRow(history.Entries[0].Seq);

        Assert.Equal("Open", page.TextAt(".slnotify-row.is-open .sn-open"));
        Assert.False(page.Has(".sn-open.sn-hot"));
    }

    [Fact]
    public void AnOpenedRowWithNothingToOpenSaysSoAndKeepsThePhoneOpen()
    {
        var page = PageScript.WithPhone();
        var history = new History();

        history.Record(Make.Toast(1, "Not enough stamina"), null, null);
        page.Install(history);
        page.Click(".is-slnotify");
        page.ClickRow(history.Entries[0].Seq);

        Assert.False(page.Has(".sn-open"));
        Assert.Equal("No popup for this notice", page.TextAt(".slnotify-row.is-open .sn-hint"));
        Assert.DoesNotContain(page.Posted(), message => message.StartsWith("open|"));
        Assert.True(page.Has(".slnotify-view.is-active"));
        Assert.Equal(0, page.Number("phone.document.closedCount"));
    }

    [Fact]
    public void FiltersShowOnlyTheirRowsWithACountEach()
    {
        var page = PageScript.WithPhone();
        var history = new History();

        history.Record(Make.Toast(1, "wish"), null, Make.Wish());
        history.Record(Make.Toast(2, "message"), null, null);
        history.MarkSeen(history.LatestMark);
        history.Record(ModToast(3, "mod"), null, null);
        page.Install(history);
        page.Click(".is-slnotify");

        Assert.Equal(new[] { "All 3", "New 1", "Popups 1", "Messages 1", "Mods 1" }, page.Texts(".sn-filters .sn-chip"));

        page.Click(".sn-chip.sn-f-popup");

        Assert.Equal(new[] { "wish" }, page.Texts(".slnotify-row .sn-title"));
        Assert.True(page.Has(".sn-chip.sn-f-popup.sn-on"));

        page.Click(".sn-chip.sn-f-unread");

        Assert.Equal(new[] { "mod" }, page.Texts(".slnotify-row .sn-title"));

        page.Click(".sn-chip.sn-f-msg");

        Assert.Equal(new[] { "message" }, page.Texts(".slnotify-row .sn-title"));
    }

    [Fact]
    public void AFilterWithNothingInItSaysSo()
    {
        var page = PageScript.WithPhone();
        var history = new History();

        history.Record(Make.Toast(1, "message"), null, null);
        page.Install(history);
        page.Click(".is-slnotify");
        page.Click(".sn-chip.sn-f-mod");

        Assert.Equal(0, page.Count(".slnotify-row"));
        Assert.Equal("Nothing in this filter", page.TextAt(".sn-et"));
    }

    [Fact]
    public void MarkAllReadReadsEverythingUpToTheNewestNotice()
    {
        var page = PageScript.WithPhone();
        var history = new History();

        history.Record(Make.Toast(1, "a"), null, null);
        history.Record(Make.Toast(2, "b"), null, null);
        page.Install(history);
        page.Click(".is-slnotify");

        Assert.Equal(2, page.Count(".slnotify-row.is-unread"));

        page.Click(".sn-readall");

        Assert.Equal(new[] { "seen|" + history.LatestMark }, page.Posted());
        Assert.Equal(0, page.Count(".slnotify-row.is-unread"));
        Assert.True(page.Has(".is-slnotify .unread-dot.slnotify-hidden"));
        Assert.True(page.Has(".sn-new.slnotify-hidden"));
    }

    [Fact]
    public void ClearListAsksFirstAndKeepCancels()
    {
        var page = PageScript.WithPhone();
        var history = new History();

        history.Record(Make.Toast(1, "a"), null, null);
        page.Install(history);
        page.Click(".is-slnotify");

        Assert.Equal("Kept with this save: 1 of " + History.Cap + " Clear list", page.TextAt(".sn-foot"));

        page.Click(".sn-clear");

        Assert.Equal("Clear the list? Keep Clear", page.TextAt(".sn-foot"));
        Assert.Empty(page.Posted());

        page.Click(".sn-no");

        Assert.True(page.Has(".sn-clear"));
        Assert.Equal(1, page.Count(".slnotify-row"));
        Assert.Empty(page.Posted());
    }

    [Fact]
    public void ConfirmingClearEmptiesTheListUpToItsNewestNotice()
    {
        var page = PageScript.WithPhone();
        var history = new History();

        history.Record(Make.Toast(1, "a"), null, null);
        page.Install(history);
        page.Click(".is-slnotify");

        Assert.Equal(1, page.Count(".slnotify-row"));

        page.Click(".sn-clear");
        page.Click(".sn-yes");

        Assert.Equal(new[] { "clear|" + history.LatestMark }, page.Posted());
        Assert.Equal(0, page.Count(".slnotify-row"));
        Assert.Equal("No notices yet", page.TextAt(".sn-et"));
        Assert.True(page.Has(".is-slnotify .unread-dot.slnotify-hidden"));
    }

    [Fact]
    public void TheHomeScreenShowsTheNewestThreeUnreadNotices()
    {
        var page = PageScript.WithPhone();
        var history = new History();

        history.Record(Make.Toast(1, "read"), null, null);
        history.MarkSeen(history.LatestMark);

        for (var i = 2; i <= 5; i++) history.Record(Make.Toast(i, "n" + i), null, null);

        page.Install(history);

        Assert.Equal(new[] { "n5", "n4", "n3" }, page.Texts(".app-grid .sn-card .sn-title"));
        Assert.True(page.Has(".app-grid.slnotify-peeking"));
        Assert.Equal("See all", page.TextAt(".sn-seeall"));
    }

    [Fact]
    public void WithNothingUnreadTheHomeScreenIsLeftAsTheGameDrawsIt()
    {
        var page = PageScript.WithPhone();
        var history = new History();

        history.Record(Make.Toast(1, "read"), null, null);
        history.MarkSeen(history.LatestMark);
        page.Install(history);

        Assert.True(page.Has(".sn-peek.slnotify-hidden"));
        Assert.False(page.Has(".app-grid.slnotify-peeking"));
    }

    [Fact]
    public void AHomeCardOpensTheListWithItsRowOpenAndRead()
    {
        var page = PageScript.WithPhone();
        var history = new History();

        history.Record(Make.Toast(1, "a"), null, null);
        history.Record(Make.Toast(2, "b"), null, null);
        page.Install(history);
        page.Click(".sn-card");

        Assert.True(page.Has(".slnotify-view.is-active"));
        Assert.Equal(new[] { "b" }, page.Texts(".slnotify-row.is-open .sn-title"));
        Assert.Equal(new[] { "read|" + history.Entries[0].Seq }, page.Posted());
        Assert.Equal(new[] { "a" }, page.Texts(".sn-card .sn-title"));
        Assert.Equal(0, page.Number("phone.document.closedCount"));
    }

    [Fact]
    public void SeeAllOpensTheListAndReadsNothing()
    {
        var page = PageScript.WithPhone();
        var history = new History();

        history.Record(Make.Toast(1, "a"), null, null);
        page.Install(history);
        page.Click(".sn-seeall");

        Assert.True(page.Has(".slnotify-view.is-active"));
        Assert.Empty(page.Posted());
    }

    [Fact]
    public void TheOpenRowAndFilterSurviveTheNextListFromThePlugin()
    {
        var page = PageScript.WithPhone();
        var history = new History();

        history.Record(Make.Toast(1, "first"), null, null);
        page.Install(history);
        page.Click(".is-slnotify");
        page.Click(".sn-chip.sn-f-msg");
        page.ClickRow(history.Entries[0].Seq);
        history.Record(Make.Toast(2, "second"), null, null);
        page.Update(history);

        Assert.Equal(new[] { "first" }, page.Texts(".slnotify-row.is-open .sn-title"));
        Assert.True(page.Has(".sn-chip.sn-f-msg.sn-on"));
    }

    [Fact]
    public void ThePhoneClosedFromOutsideComesBackWithoutTheListOpen()
    {
        var page = PageScript.WithPhone();

        page.Install(new History());
        page.Click(".is-slnotify");
        page.Click(".app-root");

        Assert.False(page.Has(".slnotify-view.is-active"));
    }

    [Fact]
    public void TheBackButtonClosesTheList()
    {
        var page = PageScript.WithPhone();

        page.Install(new History());
        page.Click(".is-slnotify");
        page.Click(".slnotify-view .back");

        Assert.False(page.Has(".slnotify-view.is-active"));
        Assert.Equal(0, page.Number("phone.document.closedCount"));
    }

    [Fact]
    public void AnAppThePhonesRedrawDroppedIsPutBackAtOnce()
    {
        var page = PageScript.WithPhone();
        var history = new History();

        history.Record(Make.Toast(1, "kept"), null, null);
        page.Install(history);
        page.Run("phone.document.querySelector('.is-slnotify').remove(); phone.document.querySelector('.slnotify-view').remove(); phone.document.querySelector('.sn-peek').remove(); phone.mutate();");

        Assert.Equal(1, page.Count(".app-dock .is-slnotify"));
        Assert.Equal(1, page.Count(".sms-panel .slnotify-view"));
        Assert.Equal(new[] { "kept" }, page.Texts(".sn-card .sn-title"));

        page.Click(".is-slnotify");

        Assert.Equal(new[] { "kept" }, page.Texts(".slnotify-row .sn-title"));
    }

    [Fact]
    public void APhoneThatLoadsItsPageAgainGetsTheAppBackOnTheNextCheck()
    {
        var page = PageScript.WithPhone();
        var history = new History();

        history.Record(Make.Toast(1, "kept"), null, null);
        page.Install(history);
        page.Run("reloadPhone(phone, {});");

        Assert.False(page.Has(".is-slnotify"));

        page.Run("runTimers();");

        Assert.Equal(new[] { "kept" }, page.Texts(".sn-card .sn-title"));

        page.Click(".is-slnotify");

        Assert.Equal(new[] { "kept" }, page.Texts(".slnotify-row .sn-title"));
    }

    [Fact]
    public void AnUnchangedListSentAgainKeepsTheRowsAsTheyAre()
    {
        var page = PageScript.WithPhone();
        var history = new History();
        var list = new PhoneList();

        history.Record(Make.Toast(1, "first"), null, null);
        history.Record(Make.Toast(2, "second"), null, null);
        page.Install(history, list);
        page.Click(".is-slnotify");
        page.Run("phone.document.querySelectorAll('.slnotify-row').forEach(function (r) { r.__mark = 7; });");
        page.Send(history, list);

        Assert.Equal(new[] { "second", "first" }, page.Texts(".slnotify-row .sn-title"));
        Assert.Equal(2, page.Number("phone.document.querySelectorAll('.slnotify-row').filter(function (r) { return r.__mark === 7; }).length"));
    }

    [Fact]
    public void ANewNoticeIsAddedOnTopAndTheOthersAreKept()
    {
        var page = PageScript.WithPhone();
        var history = new History();
        var list = new PhoneList();

        history.Record(Make.Toast(1, "first"), null, null);
        page.Install(history, list);
        page.Click(".is-slnotify");
        page.Run("phone.document.querySelector('.slnotify-row').__mark = 7;");
        history.Record(Make.Toast(2, "second"), null, null);
        page.Send(history, list);

        Assert.Equal(new[] { "second", "first" }, page.Texts(".slnotify-row .sn-title"));
        Assert.True(page.Is("phone.document.querySelectorAll('.slnotify-row')[1].__mark === 7"));
    }

    [Fact]
    public void ARowSentByANumberThePageDoesNotKnowAsksForTheWholeListAndKeepsTheRows()
    {
        var page = PageScript.WithPhone();
        var history = new History();
        var list = new PhoneList();

        history.Record(Make.Toast(1, "first"), null, null);
        page.Install(history, list);
        page.Click(".is-slnotify");
        page.Run(PageCalls.Set("{\"rows\":[999]}"));

        Assert.Contains("resync", page.Posted());
        Assert.Equal(new[] { "first" }, page.Texts(".slnotify-row .sn-title"));

        page.Send(history, list);

        Assert.Equal(new[] { "first" }, page.Texts(".slnotify-row .sn-title"));
    }

    [Fact]
    public void ANoticeRaisedAgainIsRedrawnWithItsNewCount()
    {
        var page = PageScript.WithPhone();
        var history = new History();
        var list = new PhoneList();

        history.Record(Make.Toast(1, "Not enough stamina"), null, null);
        page.Install(history, list);
        page.Click(".is-slnotify");
        history.Record(Make.Toast(1, "Not enough stamina", isMerge: true), null, null);
        page.Send(history, list);

        Assert.Equal(new[] { "x2" }, page.Texts(".slnotify-row .sn-count"));
    }

    [Fact]
    public void AnAppPartThisPageDidNotMakeIsReplaced()
    {
        var page = new PageScript("var phone = addPhone({}); phone.document.querySelector('.app-dock').appendChild(new FakeElement('div', 'app is-slnotify'));");
        var history = new History();

        history.Record(Make.Toast(1, "kept"), null, null);
        page.Install(history);
        page.Click(".is-slnotify");

        Assert.Equal(1, page.Count(".app-dock .is-slnotify"));
        Assert.True(page.Has(".slnotify-view.is-active"));
        Assert.Equal(new[] { "kept" }, page.Texts(".slnotify-row .sn-title"));
    }

    [Fact]
    public void AnUpdatedHistoryRedrawsTheList()
    {
        var page = PageScript.WithPhone();
        var history = new History();

        history.Record(Make.Toast(1, "first"), null, null);
        page.Install(history);
        page.Click(".is-slnotify");
        history.Record(Make.Toast(2, "second"), null, null);
        page.Update(history);

        Assert.Equal(new[] { "second", "first" }, page.Texts(".slnotify-row .sn-title"));
    }

    [Fact]
    public void APageThatLostTheScriptAsksForItAndOneThatHasItStaysQuiet()
    {
        var page = PageScript.WithPhone();

        page.Probe();

        Assert.Equal(new[] { "noscript" }, page.Posted());

        page.Install(null);
        page.Probe();
        page.Update(null);

        Assert.Equal(new[] { "noscript" }, page.Posted());
    }

    [Fact]
    public void AFrameFromAnotherSiteIsPassedOver()
    {
        var page = new PageScript("addForeignFrame(); var phone = addPhone({});");

        page.Install(null);

        Assert.True(page.Has(".is-slnotify"));
        Assert.Empty(page.Posted());
    }
}
