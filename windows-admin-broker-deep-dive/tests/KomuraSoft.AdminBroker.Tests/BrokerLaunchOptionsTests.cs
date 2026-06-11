using Xunit;

namespace KomuraSoft.AdminBroker.Tests;

public class BrokerLaunchOptionsTests
{
    private static readonly string[] ValidArgs =
    [
        "--pipe", "myapp-broker-test",
        "--client-pid", "1234",
        "--client-sid", "S-1-5-21-1-2-3-1001"
    ];

    [Fact]
    public void Parse_AllArguments_Succeeds()
    {
        BrokerLaunchOptions options = BrokerLaunchOptions.Parse(ValidArgs);

        Assert.Equal("myapp-broker-test", options.PipeName);
        Assert.Equal(1234, options.ExpectedClientProcessId);
        Assert.Equal("S-1-5-21-1-2-3-1001", options.ClientUserSid);
    }

    [Fact]
    public void Parse_MissingPipe_Throws()
    {
        string[] args = ["--client-pid", "1234", "--client-sid", "S-1-5-18"];

        var ex = Assert.Throws<ArgumentException>(() => BrokerLaunchOptions.Parse(args));
        Assert.Contains("--pipe", ex.Message);
    }

    [Fact]
    public void Parse_MissingClientPid_Throws()
    {
        string[] args = ["--pipe", "p", "--client-sid", "S-1-5-18"];

        var ex = Assert.Throws<ArgumentException>(() => BrokerLaunchOptions.Parse(args));
        Assert.Contains("--client-pid", ex.Message);
    }

    [Fact]
    public void Parse_MissingClientSid_Throws()
    {
        string[] args = ["--pipe", "p", "--client-pid", "1234"];

        var ex = Assert.Throws<ArgumentException>(() => BrokerLaunchOptions.Parse(args));
        Assert.Contains("--client-sid", ex.Message);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("0")]
    [InlineData("-5")]
    public void Parse_InvalidClientPid_Throws(string pid)
    {
        string[] args = ["--pipe", "p", "--client-pid", pid, "--client-sid", "S-1-5-18"];

        var ex = Assert.Throws<ArgumentException>(() => BrokerLaunchOptions.Parse(args));
        Assert.Contains("Invalid client PID", ex.Message);
    }

    [Fact]
    public void Parse_UnknownArgument_Throws()
    {
        // 昇格境界の内側では「とりあえず頑張って解釈する」をしない（記事 11 章）
        string[] args = [.. ValidArgs, "--unexpected", "value"];

        var ex = Assert.Throws<ArgumentException>(() => BrokerLaunchOptions.Parse(args));
        Assert.Contains("Unknown argument", ex.Message);
    }

    [Fact]
    public void Parse_OptionWithoutValue_Throws()
    {
        string[] args = ["--pipe"];

        var ex = Assert.Throws<ArgumentException>(() => BrokerLaunchOptions.Parse(args));
        Assert.Contains("A value is required", ex.Message);
    }
}
