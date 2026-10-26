using Notifications.Core;
using Xunit;

namespace Notifications.Tests;

/// <summary>
/// The game builds a new phone page each time the phone opens and shows it two frames later; the dock icon, its badge
/// and the newest notices must be on it by then (drawn in the next animation frame), not on the next check. None of
/// these tests run the check unless they say so.
/// </summary>
public class PhoneOpeningTests
{
    /// <summary>The script in a root page with no phone yet, holding two unread notices.</summary>
    private static PageScript Installed()
    {
        var page = new PageScript(PageScript.Resource("Notifications.Tests.FrameFake.js"));
        var history = new History();

        history.Record(Make.Toast(1, "a"), null, null);
        history.Record(Make.Toast(2, "b"), null, null);
        page.Install(history);

        return page;
    }

    /// <summary>The phone opening: Root makes the frame, the page arrives a frame later, and the game builds it.</summary>
    private static void OpenPhone(PageScript page)
    {
        page.Run("var frame = openFrame(PHONE_URL); flush(); nextFrame();");
        page.Run("var doc = arrive(frame); nextFrame(); buildPhone(doc); flush(); nextFrame();");
    }

    private static void AssertTheAppIsDrawn(PageScript page)
    {
        Assert.Equal(1, page.Count(".app-dock .is-slnotify"));
        Assert.Equal("2", page.TextAt(".is-slnotify .unread-dot"));
        Assert.False(page.Has(".is-slnotify .unread-dot.slnotify-hidden"));
        Assert.Equal(2, page.Count(".sn-peek .sn-card"));
        Assert.True(page.Has(".sms-panel .slnotify-view"));
    }

    [Fact]
    public void A_phone_that_just_opened_has_the_icon_badge_and_newest_notices_before_the_next_paint()
    {
        var page = Installed();

        OpenPhone(page);

        AssertTheAppIsDrawn(page);
    }

    [Fact]
    public void A_phone_page_that_is_already_built_when_it_arrives_is_drawn_by_the_next_frame()
    {
        var page = Installed();

        page.Run("var frame = openFrame(PHONE_URL); flush(); buildPhone(arrive(frame)); nextFrame();");

        AssertTheAppIsDrawn(page);
    }

    [Fact]
    public void Every_new_phone_page_is_drawn_again()
    {
        var page = Installed();

        OpenPhone(page);
        page.Run("frame.remove(); flush(); var again = openFrame(PHONE_URL); flush(); buildPhone(arrive(again)); nextFrame();");

        AssertTheAppIsDrawn(page);
    }

    [Fact]
    public void A_phone_page_loaded_into_the_frame_again_is_watched_again()
    {
        var page = Installed();

        OpenPhone(page);
        page.Run("navigate(frame, PHONE_URL + '?t=2'); flush(); var reloaded = arrive(frame); fireLoad(frame); flush(); buildPhone(reloaded); flush(); nextFrame();");

        AssertTheAppIsDrawn(page);
    }

    [Fact]
    public void A_change_on_the_phone_that_leaves_the_app_in_place_asks_for_no_redraw()
    {
        var page = Installed();

        OpenPhone(page);
        page.Run("phone.document.body.appendChild(phone.document.createElement('div')); flush();");

        Assert.Equal(0, page.Number("frameCallbacks.length"));
        AssertTheAppIsDrawn(page);
    }

    [Fact]
    public void Many_changes_in_one_frame_put_the_app_back_in_one_redraw()
    {
        var page = Installed();

        OpenPhone(page);
        page.Run("phone.document.querySelector('.is-slnotify').remove(); flush();");
        page.Run("for (var i = 0; i < 5; i++) { phone.document.body.appendChild(phone.document.createElement('div')); flush(); }");

        Assert.Equal(1, page.Number("frameCallbacks.length"));
        Assert.False(page.Has(".is-slnotify"));

        page.Run("nextFrame();");

        AssertTheAppIsDrawn(page);
    }

    [Fact]
    public void The_watcher_and_the_check_together_draw_one_app()
    {
        var page = Installed();

        OpenPhone(page);
        page.Run("runTimers(); flush(); nextFrame(); runTimers(); flush();");

        AssertTheAppIsDrawn(page);
    }

    [Fact]
    public void Other_pages_are_left_alone()
    {
        var page = Installed();

        page.Run("var frame = openFrame('https://ui.local/UI/Bag/Bag.html'); flush(); var bag = buildPhone(arrive(frame)); nextFrame(); runTimers(); flush();");

        Assert.False(page.Is("bag.querySelector('.is-slnotify')"));
    }
}
