using MiscreatedServerConfigurator.Core.Hosting;

namespace MiscreatedServerConfigurator.Core.Tests;

public sealed class ConfigWarningTests
{
    [Fact]
    public void Template_warns_about_the_placeholder_password_and_the_empty_name()
    {
        var warnings = ConfigWarnings.Evaluate(HostingConfigSession.Load(ServerTemplate.LoadText()));
        var ids = warnings.Select(warning => warning.Id).ToArray();

        Assert.Contains("http_password.placeholder", ids);
        Assert.Contains("sv_servername.missing", ids);
        Assert.DoesNotContain("passwords.match", ids);
    }

    [Fact]
    public void Matching_passwords_are_reported_without_echoing_the_secret()
    {
        const string secret = "super-secret-value";
        var session = HostingConfigSession.Load("http_password=\nsv_servername=\"Named\"\n");
        session.SetValue("http_password", secret);
        session.SetEnabled("sv_password", true);
        session.SetValue("sv_password", "\"" + secret + "\"");

        var warnings = ConfigWarnings.Evaluate(session);
        var match = Assert.Single(warnings, warning => warning.Id == "passwords.match");

        Assert.DoesNotContain(secret, match.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(warnings, warning => warning.Id == "http_password.placeholder");
    }

    [Fact]
    public void Different_passwords_do_not_warn()
    {
        var session = HostingConfigSession.Load("http_password=one\nsv_servername=\"Named\"\n");
        session.SetEnabled("sv_password", true);
        session.SetValue("sv_password", "two");

        var warnings = ConfigWarnings.Evaluate(session);

        Assert.DoesNotContain(warnings, warning => warning.Id == "passwords.match");
        Assert.DoesNotContain(warnings, warning => warning.Id == "http_password.missing");
        Assert.DoesNotContain(warnings, warning => warning.Id == "sv_servername.missing");
    }
}
