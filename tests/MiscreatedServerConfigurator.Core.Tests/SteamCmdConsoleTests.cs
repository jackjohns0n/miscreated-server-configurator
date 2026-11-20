using MiscreatedServerConfigurator.Core.Setup;

namespace MiscreatedServerConfigurator.Core.Tests;

public sealed class SteamCmdConsoleTests
{
    [Fact]
    public void Carriage_return_replaces_the_progress_line_and_reads_the_percentage()
    {
        var reader = new SteamCmdConsoleReader();
        var started = reader.Push("[----] Verifying installation...\n");
        var held = reader.Push("Update state (0x61) downloading, progress: 12.34 (1 / 8)\r");
        var next = reader.Push("Update state (0x61) downloading, progress: 50.00 (4 / 8)\r");
        var last = reader.Flush();

        Assert.False(started[0].ReplaceProgress);
        Assert.Equal("[----] Verifying installation...", started[0].Text);
        Assert.Null(started[0].Percent);
        Assert.Empty(held);
        Assert.True(next[0].ReplaceProgress);
        Assert.Equal(12.34, next[0].Percent);
        Assert.True(last[0].ReplaceProgress);
        Assert.Equal(50, last[0].Percent);
    }

    [Fact]
    public void Carriage_return_then_line_feed_is_one_new_line()
    {
        var reader = new SteamCmdConsoleReader();
        var held = reader.Push("hello\r");
        var rest = reader.Push("\nworld\n");

        Assert.Empty(held);
        Assert.Equal(2, rest.Count);
        Assert.False(rest[0].ReplaceProgress);
        Assert.Equal("hello", rest[0].Text);
        Assert.False(rest[1].ReplaceProgress);
        Assert.Equal("world", rest[1].Text);
    }

    [Fact]
    public void A_percentage_split_across_chunks_is_kept_until_the_carriage_return()
    {
        var reader = new SteamCmdConsoleReader();
        var partial = reader.Push("progress: 12.");
        reader.Push("50 (1 / 2)\r");
        var finished = reader.Flush();

        Assert.Empty(partial);
        Assert.True(finished[0].ReplaceProgress);
        Assert.Equal(12.5, finished[0].Percent);
    }

    [Fact]
    public void Content_log_state_lines_are_recognized()
    {
        const string Installed = "AppID 302200 state changed : Fully Installed";
        const string Downloading = "AppID 302200 state changed : Downloading, progress 12.34 (1 / 9)";

        Assert.True(SteamCmdProgress.IsStateLine(Installed));
        Assert.False(SteamCmdProgress.TryGetPercent(Installed, out _));
        Assert.True(SteamCmdProgress.IsStateLine(Downloading));
        Assert.True(SteamCmdProgress.TryGetPercent(Downloading, out var percent));
        Assert.Equal(12.34, percent);
        Assert.True(SteamCmdProgress.IsStateLine("Error! App '302200' state is 0x202 after update job."));
    }

    [Fact]
    public void Flush_emits_a_progress_line_that_ended_on_a_carriage_return()
    {
        var reader = new SteamCmdConsoleReader();
        reader.Push("progress: 1.00\r");
        var last = reader.Flush();

        Assert.True(last[0].ReplaceProgress);
        Assert.Equal(1, last[0].Percent);
        var held = new SteamCmdConsoleReader();
        held.Push("progress: 99.5");
        var flushed = held.Flush();
        Assert.False(flushed[0].ReplaceProgress);
        Assert.Equal(99.5, flushed[0].Percent);
    }
}
