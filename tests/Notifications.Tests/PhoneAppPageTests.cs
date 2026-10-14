using Notifications.Core;
using Xunit;

namespace Notifications.Tests;

/// <summary>The phone app as the player sees it: notifications.js run on a fake phone page.</summary>
public class PhoneAppPageTests
{
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

        Assert.Equal("通知", page.TextAt(".is-slnotify .app-label"));
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
    }

    [Fact]
    public void TheListShowsEveryNoticeNewestFirstWithItsTimeSourceAndCount()
    {
        var page = PageScript.WithPhone("第{0}天");
        var history = new History();

        history.Record(Make.Toast(1, "Wish fulfilled"), new GameStamp(3, 7), Make.Wish());
        history.Record(new ToastNote(2, new Note("Storage Hub", "Not enough stamina", "Rest first", NoteKind.Warning), Make.T0, false, false), new GameStamp(12, 18), null);
        history.Record(new ToastNote(2, new Note("Storage Hub", "Not enough stamina", "Rest first", NoteKind.Warning), Make.T0, false, true), new GameStamp(12, 18), null);
        history.Record(new ToastNote(2, new Note("Storage Hub", "Not enough stamina", "Rest first", NoteKind.Warning), Make.T0, false, true), new GameStamp(12, 18), null);

        page.Install(history);

        Assert.Equal(new[] { "Not enough stamina", "Wish fulfilled" }, page.Texts(".slnotify-row .row-nm"));
        Assert.Equal(new[] { "Rest first" }, page.Texts(".slnotify-row .row-preview"));
        Assert.Equal(new[] { "第12天 18:00", "第3天 07:00" }, page.Texts(".slnotify-row .row-time"));
        Assert.Equal(new[] { "Storage Hub", "Game" }, page.Texts(".slnotify-row .slnotify-source"));
        Assert.Equal(new[] { "x3" }, page.Texts(".slnotify-row .slnotify-count"));
    }

    [Fact]
    public void AnEmptyHistorySaysSo()
    {
        var page = PageScript.WithPhone();

        page.Install(new History());

        Assert.Equal("No notifications yet", page.TextAt(".slnotify-view .empty-text"));
        Assert.False(page.Has(".list-empty.slnotify-hidden"));
        Assert.True(page.Has(".list-rows.slnotify-hidden"));
    }

    [Fact]
    public void TheIconShowsADotWhileSomethingIsUnreadAndOpeningTheListClearsIt()
    {
        var page = PageScript.WithPhone();
        var history = new History();

        history.Record(Make.Toast(1, "a"), null, null);
        history.Record(Make.Toast(2, "b"), null, null);
        page.Install(history);

        Assert.False(page.Has(".is-slnotify .unread-dot.slnotify-hidden"));

        page.Click(".is-slnotify");

        Assert.True(page.Has(".slnotify-view.is-active"));
        Assert.True(page.Has(".is-slnotify .unread-dot.slnotify-hidden"));
        Assert.Equal(new[] { "seen|" + history.LatestMark }, page.Posted());
    }

    [Fact]
    public void NothingUnreadShowsNoDot()
    {
        var page = PageScript.WithPhone();
        var history = new History();

        history.Record(Make.Toast(1, "a"), null, null);
        history.MarkSeen(history.LatestMark);
        page.Install(history);

        Assert.True(page.Has(".is-slnotify .unread-dot.slnotify-hidden"));
    }

    [Fact]
    public void ClickingARowThatCanOpenItsPopupAsksForItAndClosesThePhoneSoThePopupIsNotUnderIt()
    {
        var page = PageScript.WithPhone();
        var history = new History();

        history.Record(Make.Toast(1, "Wish fulfilled"), null, Make.Wish());
        page.Install(history);
        page.Click(".is-slnotify");
        page.ClickRow(history.Entries[0].Seq);

        Assert.Equal("open|" + history.Entries[0].Seq, page.Posted()[^1]);
        Assert.False(page.Has(".slnotify-view.is-active"));
        Assert.Equal(1, page.Number("phone.document.closedCount"));
    }

    [Fact]
    public void ClickingARowWithNothingToOpenDoesNothing()
    {
        var page = PageScript.WithPhone();
        var history = new History();

        history.Record(Make.Toast(1, "Not enough stamina"), null, null);
        page.Install(history);
        page.Click(".is-slnotify");
        page.ClickRow(history.Entries[0].Seq);

        Assert.DoesNotContain(page.Posted(), message => message.StartsWith("open|"));
        Assert.True(page.Has(".slnotify-view.is-active"));
        Assert.Equal(0, page.Number("phone.document.closedCount"));
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
        page.Run("phone.document.querySelector('.is-slnotify').remove(); phone.document.querySelector('.slnotify-view').remove(); phone.mutate();");

        Assert.Equal(1, page.Count(".app-dock .is-slnotify"));
        Assert.Equal(new[] { "kept" }, page.Texts(".slnotify-row .row-nm"));
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

        Assert.Equal(new[] { "kept" }, page.Texts(".slnotify-row .row-nm"));
    }

    [Fact]
    public void AnUpdatedHistoryRedrawsTheList()
    {
        var page = PageScript.WithPhone();
        var history = new History();

        history.Record(Make.Toast(1, "first"), null, null);
        page.Install(history);
        history.Record(Make.Toast(2, "second"), null, null);
        page.Update(history);

        Assert.Equal(new[] { "second", "first" }, page.Texts(".slnotify-row .row-nm"));
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
