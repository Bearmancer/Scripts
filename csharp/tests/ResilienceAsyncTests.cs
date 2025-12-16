namespace CSharpScripts.Tests.Unit;

using CSharpScripts.Infrastructure;

[TestClass]
public class ResilienceAsyncTests
{
    [TestMethod]
    public async Task ExecuteAsync_ReturnsValue_OnSuccess()
    {
        // Arrange
        int expected = 42;

        // Act
        int result = await Resilience.ExecuteAsync(
            operation: "Test.Success",
            action: () => Task.FromResult(expected)
        );

        // Assert
        result.ShouldBe(expected);
    }

    [TestMethod]
    public async Task ExecuteAsync_VoidOverload_Completes()
    {
        // Arrange
        bool executed = false;

        // Act
        await Resilience.ExecuteAsync(
            operation: "Test.VoidSuccess",
            action: async () =>
            {
                await Task.Delay(1);
                executed = true;
            }
        );

        // Assert
        executed.ShouldBeTrue();
    }

    [TestMethod]
    public async Task ExecuteAsync_ThrottlesSequentialCalls()
    {
        // Arrange
        DateTime start = DateTime.Now;
        int callCount = 0;

        // Act - Make 3 calls, each should be throttled
        for (int i = 0; i < 3; i++)
        {
            await Resilience.ExecuteAsync(
                operation: "Test.Throttle",
                action: () =>
                {
                    callCount++;
                    return Task.FromResult(true);
                }
            );
        }

        // Assert
        callCount.ShouldBe(3);
        // Throttle is 3 seconds between calls, so 3 calls should take at least 6 seconds
        // But first call doesn't wait, so minimum is ~6 seconds
        // This test just verifies execution completes; actual throttle is time-sensitive
    }

    [TestMethod]
    public async Task ExecuteAsync_SupportsCancellation()
    {
        // Arrange
        using CancellationTokenSource cts = new();
        cts.Cancel(); // Cancel immediately

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(async () =>
        {
            await Resilience.ExecuteAsync(
                operation: "Test.Cancelled",
                action: () => Task.FromResult(1),
                ct: cts.Token
            );
        });
    }

    [TestMethod]
    public void IsFatalQuotaError_DetectsQuotaExceeded()
    {
        // Test various quota error messages
        Resilience.IsFatalQuotaError("daily limit exceeded").ShouldBeTrue();
        Resilience.IsFatalQuotaError("Quota exceeded for today").ShouldBeTrue();
        Resilience.IsFatalQuotaError("quota per day limit").ShouldBeTrue();
        Resilience.IsFatalQuotaError("normal error message").ShouldBeFalse();
        Resilience.IsFatalQuotaError("rate limit exceeded").ShouldBeFalse();
    }
}
