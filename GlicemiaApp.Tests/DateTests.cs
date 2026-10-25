using GlicemiaApp.Core;
using Xunit;

public class MeasuredAtTests
{
    static readonly DateTime Now = DateTime.UtcNow;

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(-1440)]
    [InlineData(-29 * 1440)]
    public void Accepts_now_and_recent_past(int minutes) =>
        Assert.Null(InputValidator.ValidateMeasuredAt(Now.AddMinutes(minutes), Now));

    [Theory]
    [InlineData(60)]
    [InlineData(-31 * 1440)]
    public void Rejects_future_and_too_old(int minutes) =>
        Assert.NotNull(InputValidator.ValidateMeasuredAt(Now.AddMinutes(minutes), Now));
}
