using Jint;
using Notifications.Core;

namespace Notifications.Tests;

/// <summary>
/// notifications.js exactly as the plugin embeds it, run in Jint on top of FakePage.js, sent with the same calls the
/// plugin makes. Tests talk to the page in JavaScript expressions and read back plain strings, numbers and booleans.
/// </summary>
internal sealed class PageScript
{
    private readonly Engine _engine = new();

    /// <summary>The fake page, then the statements that draw what the test needs in it (the phone is usually <c>phone</c>).</summary>
    public PageScript(string setup)
    {
        _engine.Execute(Resource("Notifications.Tests.FakePage.js"));
        _engine.Execute(setup);
    }

    /// <summary>A phone frame with this day label, as the global <c>phone</c>.</summary>
    public static PageScript WithPhone(string dayLabel = "DAY {0}") => new($"var phone = addPhone({{ dayLabel: '{dayLabel}' }});");

    public void Install(History? history, bool isChinese = false) => Run(PageCalls.Install(Resource("Notifications.notifications.js"), PhoneView.Json(history, isChinese)));

    /// <summary>The script with a view drawn from the game's words and pictures, on this in-game day.</summary>
    public void Install(History? history, IViewWords words, int? today) => Run(PageCalls.Install(Resource("Notifications.notifications.js"), PhoneView.Json(history, words, today)));

    public void Update(History? history, bool isChinese = false) => Run(PageCalls.Set(PhoneView.Json(history, isChinese)));

    public void Probe() => Run(PageCalls.Probe);

    /// <summary>Clicks the first element under the phone that a selector finds.</summary>
    public void Click(string selector) => Run($"phone.document.querySelector('{selector}').click();");

    public void ClickRow(long seq) =>
        Run($"phone.document.querySelectorAll('.slnotify-row').filter(function (r) {{ return r.getAttribute('data-seq') === '{seq}'; }})[0].click();");

    /// <summary>Every message the page sent the plugin, without the mod's prefix.</summary>
    public string[] Posted() =>
        Text("posted.join('\\n')").Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(message => message.Substring(PageMessage.Prefix.Length)).ToArray();

    /// <summary>The text under the first element under the phone that a selector finds, or "none".</summary>
    public string TextAt(string selector) => Text($"(function (e) {{ return e ? textOf(e) : 'none'; }})(phone.document.querySelector('{selector}'))");

    public string[] Texts(string selector) =>
        Text($"phone.document.querySelectorAll('{selector}').map(function (e) {{ return textOf(e); }}).join('\\n')").Split('\n', StringSplitOptions.RemoveEmptyEntries);

    public bool Has(string selector) => Is($"phone.document.querySelector('{selector}')");

    public int Count(string selector) => (int)Number($"phone.document.querySelectorAll('{selector}').length");

    public void Run(string statements) => _engine.Execute(statements);

    public string Text(string expression) => _engine.Evaluate($"String({expression})").AsString();

    public bool Is(string expression) => _engine.Evaluate($"!!({expression})").AsBoolean();

    public double Number(string expression) => _engine.Evaluate($"Number({expression})").AsNumber();

    internal static string Resource(string name)
    {
        using var stream = typeof(PageScript).Assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException(name + " is not embedded in the test assembly");
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }
}
