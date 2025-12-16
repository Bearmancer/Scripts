namespace CSharpScripts.Tests.Unit;

[TestClass]
public class ResilienceTests
{
    [TestMethod]
    public void Execute_SuccessfulOperation_ReturnsResult()
    {
        // Arrange
        var expectedResult = 42;

        // Act
        var result = Resilience.Execute(operation: "Test.Operation", action: () => expectedResult);

        // Assert
        result.ShouldBe(expectedResult);
    }

    [TestMethod]
    public void Execute_Action_CompletesSuccessfully()
    {
        // Arrange
        var executed = false;

        // Act
        Resilience.Execute(operation: "Test.VoidOperation", action: () => executed = true);

        // Assert
        executed.ShouldBeTrue();
    }

    [TestMethod]
    public async Task ExecuteAsync_SuccessfulOperation_ReturnsResult()
    {
        // Arrange
        var expectedResult = "async result";

        // Act
        var result = await Resilience.ExecuteAsync(
            operation: "Test.AsyncOperation",
            action: () => Task.FromResult(expectedResult)
        );

        // Assert
        result.ShouldBe(expectedResult);
    }

    [TestMethod]
    public async Task ExecuteAsync_VoidAction_CompletesSuccessfully()
    {
        // Arrange
        var executed = false;

        // Act
        await Resilience.ExecuteAsync(
            operation: "Test.AsyncVoidOperation",
            action: () =>
            {
                executed = true;
                return Task.CompletedTask;
            }
        );

        // Assert
        executed.ShouldBeTrue();
    }

    [TestMethod]
    public void Execute_CancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        Should.Throw<OperationCanceledException>(() =>
            Resilience.Execute(
                operation: "Test.CancelledOperation",
                action: () => 42,
                ct: cts.Token
            )
        );
    }
}
