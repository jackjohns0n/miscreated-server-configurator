using MiscreatedServerConfigurator.Core.Hosting;

namespace MiscreatedServerConfigurator.Core.Tests;

public sealed class SteamUgcListTests
{
    [Fact]
    public void Parse_trims_around_commas_and_keeps_order_and_duplicates()
    {
        var ids = SteamUgcList.Parse("111, 222,333, 111,");

        Assert.Equal(new[] { "111", "222", "333", "111" }, ids);
    }

    [Fact]
    public void Format_joins_with_commas_and_no_spaces()
    {
        var value = SteamUgcList.Format(new[] { "10", " 20 ", "30" });

        Assert.Equal("10,20,30", value);
        Assert.DoesNotContain(' ', value);
    }

    [Fact]
    public void Format_rejects_an_id_that_contains_a_space()
    {
        var error = Assert.Throws<ArgumentException>(() => SteamUgcList.Format(new[] { "12 34" }));

        Assert.Contains("space", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("12345", "12345")]
    [InlineData("  99  ", "99")]
    [InlineData("https://steamcommunity.com/sharedfiles/filedetails/?id=299740123", "299740123")]
    [InlineData("https://steamcommunity.com/workshop/filedetails/?id=42&searchtext=test", "42")]
    public void TryExtractId_accepts_digits_and_workshop_urls(string input, string expected)
    {
        var parsed = SteamUgcList.TryExtractId(input, out var id);

        Assert.True(parsed);
        Assert.Equal(expected, id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-id")]
    [InlineData("id=12345")]
    [InlineData("https://example.com/no-id-here")]
    public void TryExtractId_rejects_text_that_is_not_an_id_or_workshop_url(string input)
    {
        Assert.False(SteamUgcList.TryExtractId(input, out _));
    }

    [Fact]
    public void TryParseUserInput_accepts_a_comma_separated_mix_of_ids_and_urls()
    {
        var parsed = SteamUgcList.TryParseUserInput(
            "10, https://steamcommunity.com/sharedfiles/filedetails/?id=20",
            out var ids,
            out var error);

        Assert.True(parsed);
        Assert.Null(error);
        Assert.Equal(new[] { "10", "20" }, ids);
    }

    [Fact]
    public void Writing_steam_ugc_uses_the_comma_separated_list()
    {
        const string text = "-- steam_ugc= - Steam UGC ids.\n";
        var session = HostingConfigSession.Load(text);

        Assert.Equal(string.Empty, session.GetRequired("steam_ugc").Value);
        Assert.False(session.GetRequired("steam_ugc").Enabled);
        Assert.Equal(text, session.ToText(UnusedCvarOutput.CommentOut));

        session.SetValue("steam_ugc", SteamUgcList.Format(new[] { "10", "20" }));
        session.SetEnabled("steam_ugc", true);

        Assert.Equal("steam_ugc=10,20\n", session.ToText(UnusedCvarOutput.CommentOut));
    }
}
