using HomeApps.Desktop;

namespace Jarvis.Desktop.Tests;

public class ReducedMotionTests
{
    private const string Variable = "HOMEAPPS_REDUCE_MOTION";

    [Theory]
    [InlineData("1", true)]
    [InlineData("0", false)]
    public void IsOn_ForcedByEnvironmentVariable(string value, bool expected)
    {
        var previous = Environment.GetEnvironmentVariable(Variable);
        try
        {
            Environment.SetEnvironmentVariable(Variable, value);

            Assert.Equal(expected, ReducedMotion.IsOn());
        }
        finally
        {
            Environment.SetEnvironmentVariable(Variable, previous);
        }
    }
}
