using MiscreatedServerConfigurator.Core.Catalog;
using MiscreatedServerConfigurator.Core.Hosting;

namespace MiscreatedServerConfigurator.Core.Tests;

public sealed class ServerNameValueTests
{
    [Theory]
    [InlineData("\"My Server\"", "My Server")]
    [InlineData("\"\"", "")]
    [InlineData("MyServer", "MyServer")]
    [InlineData("\"  spaced  \"", "  spaced  ")]
    public void ToPlain_removes_one_surrounding_pair_of_quotes(string fileValue, string plain)
    {
        Assert.Equal(plain, ServerNameValue.ToPlain(fileValue));
    }

    [Theory]
    [InlineData("My Server", "\"My Server\"")]
    [InlineData("\"My Server\"", "\"My Server\"")]
    [InlineData("", "\"\"")]
    [InlineData("\"\"", "\"\"")]
    public void ToFile_wraps_the_name_once(string input, string fileValue)
    {
        Assert.Equal(fileValue, ServerNameValue.ToFile(input));
    }

    [Fact]
    public void Existing_quoted_name_round_trips()
    {
        const string text = "sv_servername=\"My Server\"\n";
        var session = HostingConfigSession.Load(text);

        Assert.Equal("\"My Server\"", session.GetRequired(ServerNameValue.Key).Value);
        Assert.Equal("My Server", ServerNameValue.ToPlain(session.GetRequired(ServerNameValue.Key).Value));
        Assert.Equal(text, session.ToText(UnusedCvarOutput.CommentOut));

        session.SetValue(ServerNameValue.Key, ServerNameValue.ToFile("Other Name"));
        Assert.Equal("sv_servername=\"Other Name\"\n", session.ToText(UnusedCvarOutput.CommentOut));

        session.SetValue(ServerNameValue.Key, ServerNameValue.ToFile("My Server"));
        Assert.Equal(text, session.ToText(UnusedCvarOutput.CommentOut));
    }

    [Fact]
    public void Empty_and_quote_only_template_values_are_not_reference_values()
    {
        Assert.False(TemplateValuePolicy.IsReference(null));
        Assert.False(TemplateValuePolicy.IsReference(string.Empty));
        Assert.False(TemplateValuePolicy.IsReference("   "));
        Assert.False(TemplateValuePolicy.IsReference("\"\""));
        Assert.False(TemplateValuePolicy.IsReference("\"   \""));
        Assert.True(TemplateValuePolicy.IsReference("CHANGE_ME"));
        Assert.True(TemplateValuePolicy.IsReference("\"Welcome\""));

        var serverName = CvarCatalog.Default.Find(ServerNameValue.Key);
        Assert.NotNull(serverName);
        Assert.Equal("\"\"", serverName.TemplateValue);
        Assert.False(TemplateValuePolicy.IsReference(serverName.TemplateValue));

        foreach (var entry in CvarCatalog.Default.Entries)
        {
            if (string.IsNullOrWhiteSpace(entry.TemplateValue) || entry.TemplateValue.Trim() == "\"\"")
            {
                Assert.False(TemplateValuePolicy.IsReference(entry.TemplateValue), entry.Key);
            }
        }
    }
}
