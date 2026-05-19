using TUnit.Core;

namespace TunitTestExamples;

// Production code under test
public class Calculator
{
    public int Add(int a, int b) => a + b;

    public int Divide(int a, int b)
    {
        if (b == 0)
            throw new ArgumentException("Divisor cannot be zero");
        return a / b;
    }
}

// Tests mirror the class name: CalculatorTests
[TestFixture]
public class CalculatorTests
{
    Calculator calculator = null!;

    [BeforeEach]
    public void Setup() => calculator = new Calculator();

    [Test]
    public void Add_WithPositiveNumbers_ReturnsSum()
    {
        int result = calculator.Add(5, 3);
        Assert.That(result).IsEqualTo(8);
    }

    [Test]
    [Arguments(1, 1, 2)]
    [Arguments(-1, 1, 0)]
    [Arguments(0, 0, 0)]
    public void Add_WithParameterized_ReturnsExpected(int a, int b, int expected)
    {
        Assert.That(calculator.Add(a, b)).IsEqualTo(expected);
    }

    [Test]
    public void Divide_ByZero_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() => calculator.Divide(10, 0));
        Assert.That(exception.Message).Contains("Divisor cannot be zero");
    }

    [Test]
    public async Task AsyncOperation_WithValidInput_CompletesSuccessfully()
    {
        await Task.Delay(10);
        Assert.That(true).IsTrue();
    }
}

