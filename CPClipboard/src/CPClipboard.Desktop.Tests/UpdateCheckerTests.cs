using CPClipboard.App;

namespace CPClipboard.Desktop.Tests;

public sealed class UpdateCheckerTests
{
    [Theory]
    [InlineData("v1.0.2", "1.0.1", true)]
    [InlineData("v1.10.0", "1.9.0", true)]
    [InlineData("v2.0.0", "1.99.0", true)]
    [InlineData("v1.0.1", "1.0.1.0", false)]
    [InlineData("v1.0.0", "1.0.1", false)]
    [InlineData("v1.0.2-beta", "1.0.1", false)]
    [InlineData("invalid", "1.0.1", false)]
    [InlineData("v1.0.2", "1.0.1+abcdef", true)]
    public void Release_versions_are_compared_numerically(string tag, string installed, bool newer)
        => Assert.Equal(newer, UpdateChecker.IsNewer(tag, installed));
}
