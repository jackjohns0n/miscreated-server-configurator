using MiscreatedServerConfigurator.Core.Hosting;

namespace MiscreatedServerConfigurator.Core.Tests;

public sealed class UnusedCvarOutputTests
{
    [Fact]
    public void Omit_drops_unused_cvar_lines_and_keeps_prose_and_live_settings()
    {
        var session = HostingConfigSession.Load(ServerTemplate.LoadText());
        var written = session.ToText(UnusedCvarOutput.Omit);

        Assert.Contains("http_password=CHANGE_ME\n", written, StringComparison.Ordinal);
        Assert.Contains("sv_servername=\"\"", written, StringComparison.Ordinal);
        Assert.Contains("Required Server Settings", written, StringComparison.Ordinal);
        Assert.Contains("Clothing first", written, StringComparison.Ordinal);
        Assert.Contains("http_password= mypassword", written, StringComparison.Ordinal);
        Assert.DoesNotContain("sv_allow_prone", written, StringComparison.Ordinal);
        Assert.DoesNotContain("g_gameRules_faction0_dmg_f0", written, StringComparison.Ordinal);
        Assert.DoesNotContain("g_gameRules_faction4_equip", written, StringComparison.Ordinal);
        Assert.DoesNotContain("steam_ugc=", written, StringComparison.Ordinal);
    }

    [Fact]
    public void Comment_mode_disabling_a_live_key_comments_it_out()
    {
        const string text = "http_password=secret\nsv_servername=\"A\"\n";
        var session = HostingConfigSession.Load(text);
        session.SetEnabled("http_password", false);

        Assert.Equal("-- http_password=secret\nsv_servername=\"A\"\n", session.ToText(UnusedCvarOutput.CommentOut));
    }

    [Fact]
    public void Omit_mode_disabling_a_live_key_removes_it()
    {
        const string text = "http_password=secret\nsv_servername=\"A\"\n";
        var session = HostingConfigSession.Load(text);
        session.SetEnabled("http_password", false);

        Assert.Equal("sv_servername=\"A\"\n", session.ToText(UnusedCvarOutput.Omit));
    }

    [Fact]
    public void Omit_removes_an_unused_unknown_key_and_keeps_a_live_one()
    {
        const string text = "-- a note about the file\ncustom_key=value\n-- sv_motd=\"\" - message\n";
        var session = HostingConfigSession.Load(text);

        var written = session.ToText(UnusedCvarOutput.Omit);

        Assert.Contains("-- a note about the file", written, StringComparison.Ordinal);
        Assert.Contains("custom_key=value", written, StringComparison.Ordinal);
        Assert.DoesNotContain("sv_motd", written, StringComparison.Ordinal);

        session.SetEnabled("custom_key", false);
        var withoutCustom = session.ToText(UnusedCvarOutput.Omit);
        Assert.DoesNotContain("custom_key", withoutCustom, StringComparison.Ordinal);
        Assert.Contains("-- custom_key=value", session.ToText(UnusedCvarOutput.CommentOut), StringComparison.Ordinal);
    }
}
