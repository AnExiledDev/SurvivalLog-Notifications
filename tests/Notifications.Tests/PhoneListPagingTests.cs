using Notifications.Core;
using Xunit;

namespace Notifications.Tests;

/// <summary>
/// A long history opens fast: the list draws its first rows, then more as the player scrolls toward its end, and every
/// row shares one click listener. The browser's IntersectionObserver is faked on the phone's page; the test decides
/// when the end of the list comes into view.
/// </summary>
public class PhoneListPagingTests
{
    // Behaves as the browser does: watching a node reports once, later, whether it is in view; scrolling to the end
    // reports the watched marker in view. settle() delivers what is waiting.
    private const string FakeObserver =
        "var observers = []; var waiting = []; var endInView = false;" +
        "phone.IntersectionObserver = function (callback, options) {" +
        "  var io = this; io.callback = callback; io.options = options; io.nodes = [];" +
        "  io.observe = function (node) { io.nodes.push(node); waiting.push({ io: io, node: node }); };" +
        "  io.unobserve = function (node) { io.nodes = io.nodes.filter(function (n) { return n !== node; }); };" +
        "  observers.push(io);" +
        "};" +
        "function settle() {" +
        "  while (waiting.length) { var w = waiting.shift(); if (w.io.nodes.indexOf(w.node) >= 0) w.io.callback([{ isIntersecting: endInView, target: w.node }]); }" +
        "}" +
        "function scrollToEnd() {" +
        "  observers.forEach(function (io) { io.callback(io.nodes.map(function (n) { return { isIntersecting: true, target: n }; })); });" +
        "  settle();" +
        "}";

    private static void Open(PageScript page)
    {
        page.Click(".is-slnotify");
        page.Run("if (typeof settle === 'function') settle();");
    }

    private static (PageScript Page, History History) LongList(int count, bool hasObserver = true)
    {
        var page = PageScript.WithPhone();
        var history = new History();

        if (hasObserver) page.Run(FakeObserver);

        for (var i = 1; i <= count; i++) history.Record(Make.Toast(i, "n" + i), null, null);

        page.Install(history);

        return (page, history);
    }

    [Fact]
    public void OpeningALongListDrawsTheFirstFortyRowsAndMoreAsItsEndComesIntoView()
    {
        var (page, _) = LongList(100);

        Open(page);

        Assert.Equal(40, page.Count(".slnotify-row"));
        Assert.Equal("n100", page.Texts(".slnotify-row .sn-title")[0]);
        Assert.True(page.Has(".sn-scroll .sn-more-rows"));
        Assert.True(page.Is("observers.length === 1 && observers[0].options.root === phone.document.querySelector('.sn-scroll')"));

        page.Run("scrollToEnd();");

        Assert.Equal(80, page.Count(".slnotify-row"));

        page.Run("scrollToEnd();");

        Assert.Equal(100, page.Count(".slnotify-row"));
        Assert.Equal("n1", page.Texts(".slnotify-row .sn-title")[99]);
        Assert.False(page.Has(".sn-more-rows"));
    }

    [Fact]
    public void AListTooShortToFillThePanelKeepsGrowingUntilItDoes()
    {
        var (page, _) = LongList(100);

        page.Run("endInView = true;");
        Open(page);

        Assert.Equal(100, page.Count(".slnotify-row"));
        Assert.False(page.Has(".sn-more-rows"));
    }

    [Fact]
    public void OpeningTheListAgainStartsFromTheFirstRows()
    {
        var (page, _) = LongList(100);

        Open(page);
        page.Run("scrollToEnd();");

        Assert.Equal(80, page.Count(".slnotify-row"));

        page.Click(".slnotify-view .back");
        Open(page);

        Assert.Equal(40, page.Count(".slnotify-row"));
    }

    [Fact]
    public void AnOpenRowFarDownTheListStaysDrawnWhenTheListOpensAgain()
    {
        var (page, _) = LongList(100);

        Open(page);
        page.Run("scrollToEnd();");
        page.ClickRow(30);
        page.Click(".slnotify-view .back");
        Open(page);

        Assert.Equal(71, page.Count(".slnotify-row"));
        Assert.True(page.Has(".slnotify-row.is-open"));
        Assert.Equal("30", page.Text("phone.document.querySelector('.slnotify-row.is-open').getAttribute('data-seq')"));
    }

    [Fact]
    public void ChangingTheFilterStartsFromTheFirstRows()
    {
        var (page, _) = LongList(100);

        Open(page);
        page.Run("scrollToEnd();");

        Assert.Equal(80, page.Count(".slnotify-row"));

        page.Click(".sn-chip.sn-f-unread");

        Assert.Equal(40, page.Count(".slnotify-row"));
    }

    [Fact]
    public void WithoutTheBrowsersObserverEveryRowIsDrawn()
    {
        var (page, _) = LongList(100, hasObserver: false);

        Open(page);

        Assert.Equal(100, page.Count(".slnotify-row"));
        Assert.False(page.Has(".sn-more-rows"));
    }

    [Fact]
    public void RowsAddNoListenersOfTheirOwnAndAClickInsideARowStillOpensIt()
    {
        var (page, _) = LongList(100);

        Open(page);

        Assert.Equal(0, page.Number("phone.document.querySelectorAll('.slnotify-row').filter(function (r) { return (r.listeners.click || []).length > 0; }).length"));

        page.Run("phone.document.querySelectorAll('.slnotify-row')[2].querySelector('.sn-title').click();");

        Assert.Equal("98", page.Text("phone.document.querySelector('.slnotify-row.is-open').getAttribute('data-seq')"));
        Assert.Contains("read|98", page.Posted());
    }
}
