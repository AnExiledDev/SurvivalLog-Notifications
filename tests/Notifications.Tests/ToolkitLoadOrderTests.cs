using Notifications.Core;
using Xunit;

namespace Notifications.Tests;

/// <summary>
/// notifications.js with Exiled's Toolkit's page API (api.js) in either order. The game can run either script first,
/// and the web view can reload and run them again in the other order; the phone app must work the same both ways,
/// with nothing the plugin sent in between lost.
/// </summary>
public class ToolkitLoadOrderTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void A_phone_already_shown_gets_the_app_and_its_clicks_reach_the_plugin_whichever_script_ran_first(bool isToolkitFirst)
    {
        var page = new PageScript("var phone = addPhone({});", isToolkitFirst);
        var history = new History();

        history.Record(Make.Toast(1, "a"), null, null);
        page.Install(history);

        if (!isToolkitFirst) page.LoadToolkit();

        page.Click(".is-slnotify");
        page.ClickRow(history.Entries[0].Seq);

        Assert.Equal(1, page.Count(".app-dock .is-slnotify"));
        Assert.Equal(new[] { "read|" + history.Entries[0].Seq }, page.Posted());
    }

    [Fact]
    public void A_list_sent_before_the_Toolkit_loads_shows_once_it_does()
    {
        var page = new PageScript("var phone = addPhone({});", isToolkitFirst: false);
        var history = new History();

        history.Record(Make.Toast(1, "kept"), null, null);
        page.Install(history);
        page.LoadToolkit();

        Assert.Equal(new[] { "kept" }, page.Texts(".sn-card .sn-title"));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void A_phone_opened_after_both_scripts_has_the_app_before_the_next_paint_whichever_ran_first(bool isToolkitFirst)
    {
        var page = new PageScript(PageScript.Resource("Notifications.Tests.FrameFake.js"), isToolkitFirst);
        var history = new History();

        history.Record(Make.Toast(1, "a"), null, null);
        page.Install(history);

        if (!isToolkitFirst) page.LoadToolkit();

        page.Run("var frame = openFrame(PHONE_URL); flush(); nextFrame(); var doc = arrive(frame); nextFrame(); buildPhone(doc); flush(); nextFrame();");

        Assert.Equal(1, page.Count(".app-dock .is-slnotify"));
        Assert.Equal("1", page.TextAt(".is-slnotify .unread-dot"));
    }
}
