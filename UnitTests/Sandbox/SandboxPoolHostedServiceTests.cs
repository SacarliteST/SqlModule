using Shouldly;
using SQLModule.Sandbox.Pooling;

namespace SQLModule.UnitTests.Sandbox;

public sealed class SandboxPoolHostedServiceTests
{
    [Fact(DisplayName = "Pool lifecycle: restart backoff растёт и ограничен настройкой")]
    public void RestartBackoff_GrowsAndNeverExceedsMaximum()
    {
        var first = SandboxPoolHostedService.CalculateRestartDelay(1, 60);
        var fourth = SandboxPoolHostedService.CalculateRestartDelay(4, 60);
        var capped = SandboxPoolHostedService.CalculateRestartDelay(30, 5);

        first.ShouldBeGreaterThanOrEqualTo(TimeSpan.FromSeconds(1));
        first.ShouldBeLessThan(TimeSpan.FromSeconds(1.2));
        fourth.ShouldBeGreaterThanOrEqualTo(TimeSpan.FromSeconds(8));
        capped.ShouldBeLessThanOrEqualTo(TimeSpan.FromSeconds(5));
    }
}
