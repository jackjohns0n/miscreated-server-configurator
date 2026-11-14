using MiscreatedServerConfigurator.Core.Hosting;

namespace MiscreatedServerConfigurator.Core.Tests;

public sealed class HostingConfigRoundTripTests
{
    [Fact]
    public void Reference_template_round_trips_in_comment_mode()
    {
        var text = ServerTemplate.LoadText();
        var session = HostingConfigSession.Load(text);

        var written = session.ToText(UnusedCvarOutput.CommentOut);

        Assert.Equal(text, written);
    }

    [Fact]
    public void Reference_template_on_disk_matches_the_embedded_copy()
    {
        var embedded = ServerTemplate.LoadText();
        var disk = File.ReadAllText(Path.Combine(RepoRoot(), "samples", "hosting.cfg"));

        Assert.Equal(27142, embedded.Length);
        Assert.Equal(disk, embedded);
        Assert.False(embedded.EndsWith('\n'));
    }

    [Fact]
    public void Reference_template_live_keys_are_only_the_two_required_settings()
    {
        var session = HostingConfigSession.Load(ServerTemplate.LoadText());
        var live = session.Document.Lines
            .Where(line => line.Kind == HostingLineKind.Assignment)
            .Select(line => line.Key + "=" + line.Value)
            .ToArray();

        Assert.Equal(new[] { "http_password=CHANGE_ME", "sv_servername=\"\"" }, live);
    }

    [Fact]
    public void Sv_password_comment_keeps_the_quoted_value_and_the_remark()
    {
        var line = FindTemplateLine("sv_password");

        Assert.Equal(HostingLineKind.CommentedAssignment, line.Kind);
        Assert.Equal("\"\"", line.Value);
        Assert.Contains("user.cfg", line.TrailingComment, StringComparison.Ordinal);
        Assert.DoesNotContain("user.cfg", line.Value, StringComparison.Ordinal);
    }

    [Fact]
    public void Lawmen_equipment_example_keeps_the_recorded_semicolon_split()
    {
        var line = FindTemplateLine("g_gameRules_faction0_equip");

        Assert.Contains("flexcap_police;frontback_blue", line.Value, StringComparison.Ordinal);
        Assert.DoesNotContain("Example equipment", line.Value, StringComparison.Ordinal);
        Assert.Contains("lawmen", line.TrailingComment, StringComparison.Ordinal);
    }

    [Fact]
    public void Steam_ugc_template_line_has_an_empty_value()
    {
        var line = FindTemplateLine("steam_ugc");

        Assert.Equal(string.Empty, line.Value);
        Assert.StartsWith("Steam UGC", line.TrailingComment, StringComparison.Ordinal);
    }

    [Fact]
    public void Enabling_a_commented_key_drops_the_trailing_remark()
    {
        const string text = "-- sv_motd=\"\" - This is the first message\n";
        var session = HostingConfigSession.Load(text);
        session.SetEnabled("sv_motd", true);
        session.SetValue("sv_motd", "\"Hello\"");

        var written = session.ToText(UnusedCvarOutput.CommentOut);

        Assert.Equal("sv_motd=\"Hello\"\n", written);
        Assert.DoesNotContain("first message", written, StringComparison.Ordinal);
    }

    [Fact]
    public void Restoring_the_original_value_restores_the_original_comment_line()
    {
        const string text = "-- sv_motd=\"\" - This is the first message\n";
        var session = HostingConfigSession.Load(text);
        session.SetEnabled("sv_motd", true);
        session.SetValue("sv_motd", "\"x\"");
        session.SetValue("sv_motd", "\"\"");
        session.SetEnabled("sv_motd", false);

        Assert.Equal(text, session.ToText(UnusedCvarOutput.CommentOut));
    }

    [Fact]
    public void Unknown_keys_and_prose_survive_a_comment_mode_save()
    {
        const string text = "-- a note about the file\ncustom_key=value\n-- sv_motd=\"\" - message\n";
        var session = HostingConfigSession.Load(text);

        Assert.Equal(text, session.ToText(UnusedCvarOutput.CommentOut));
        Assert.Equal("value", session.GetRequired("custom_key").Value);
    }

    [Fact]
    public void Repeated_keys_use_the_last_live_value_and_write_every_line()
    {
        const string text = "schedule_shutdown_utc=1\nschedule_shutdown_utc=2\n";
        var session = HostingConfigSession.Load(text);

        Assert.Equal("2", session.GetRequired("schedule_shutdown_utc").Value);
        session.SetValue("schedule_shutdown_utc", "3");

        Assert.Equal("schedule_shutdown_utc=3\nschedule_shutdown_utc=3\n", session.ToText(UnusedCvarOutput.CommentOut));
    }

    [Fact]
    public void Crlf_and_a_missing_final_newline_are_preserved()
    {
        const string crlf = "http_password=secret\r\nsv_servername=\"A\"\r\n";
        Assert.Equal(crlf, HostingConfigSession.Load(crlf).ToText(UnusedCvarOutput.CommentOut));

        const string noNewline = "http_password=secret";
        Assert.Equal(noNewline, HostingConfigSession.Load(noNewline).ToText(UnusedCvarOutput.CommentOut));
    }

    [Fact]
    public void A_value_cannot_contain_a_newline()
    {
        var session = HostingConfigSession.Load("sv_servername=\"\"\n");
        session.SetValue("sv_servername", "\"Hello\nWorld\"");

        Assert.Equal("\"HelloWorld\"", session.GetRequired("sv_servername").Value);
    }

    [Fact]
    public void Newly_enabled_catalog_key_is_appended()
    {
        const string text = "http_password=secret\n";
        var session = HostingConfigSession.Load(text);
        session.SetValue("sv_maxplayers", "32");
        session.SetEnabled("sv_maxplayers", true);

        Assert.Equal("http_password=secret\n\nsv_maxplayers=32\n", session.ToText(UnusedCvarOutput.CommentOut));
    }

    [Theory]
    [InlineData("wm_timeOffset=-1 - none", "wm_timeOffset", "-1", "none")]
    [InlineData("sv_servername=\"My Server\" - second", "sv_servername", "\"My Server\"", "second")]
    [InlineData("steam_ugc= - Workshop ids", "steam_ugc", "", "Workshop ids")]
    public void Commented_assignments_split_the_remark(string body, string key, string value, string remark)
    {
        var parsed = HostingSyntax.TryParseAssignment(body, splitTrailingComment: true, out var parsedKey, out var parsedValue, out var trailing);

        Assert.True(parsed);
        Assert.Equal(key, parsedKey);
        Assert.Equal(value, parsedValue);
        Assert.Equal(remark, trailing);
    }

    [Fact]
    public void Live_lines_keep_a_hyphen_inside_the_value()
    {
        var parsed = HostingSyntax.TryParseAssignment(
            "sv_motd=Hello - welcome",
            splitTrailingComment: false,
            out var key,
            out var value,
            out var trailing);

        Assert.True(parsed);
        Assert.Equal("sv_motd", key);
        Assert.Equal("Hello - welcome", value);
        Assert.Null(trailing);
    }

    private static HostingLine FindTemplateLine(string key)
    {
        var line = HostingConfigDocument.Parse(ServerTemplate.LoadText()).Lines.Single(item => item.Key == key);
        return line;
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "MiscreatedServerConfigurator.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate the repository root from the test output directory.");
    }
}
