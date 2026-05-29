using FluentAssertions;
using TUnit;

namespace Scripts.Tests.SignOff;

internal sealed class TestSuiteHealthTests
{
    [Test]
    public void Test_Directories_Exist_And_Contain_Tests()
    {
        var testDirs = new[]
        {
            @"C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Environment",
            @"C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Guards",
            @"C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Logging",
            @"C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Language",
            @"C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Repositories",
            @"C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\StateManager",
            @"C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\DbContext",
            @"C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\SignOff",
        };

        var missing = new List<string>();
        foreach (var dir in testDirs)
        {
            if (!Directory.Exists(dir))
                missing.Add(dir);
        }

        missing.Should().BeEmpty(
            $"All test directories must exist. Missing: {string.Join(", ", missing)}"
        );
    }

    [Test]
    public void Each_Test_Directory_Has_At_Least_One_Cs_File()
    {
        var testDirs = new[]
        {
            @"C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Environment",
            @"C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Guards",
            @"C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Logging",
            @"C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Language",
        };

        var empty = new List<string>();
        foreach (var dir in testDirs)
        {
            if (Directory.Exists(dir) && !Directory.EnumerateFiles(dir, "*.cs").Any())
                empty.Add(dir);
        }

        empty.Should().BeEmpty(
            $"Each test directory must contain at least one .cs test file. Empty: {string.Join(", ", empty)}"
        );
    }

    [Test]
    public void LogDirectory_Points_To_UserProfile_Cache()
    {
        var expectedBase = Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile),
            ".cache", "logs", "scripts"
        );

        var logDir = CSharpScripts.Core.Paths.LogDirectory;

        logDir.Should().Be(expectedBase,
            $"LogDirectory must point to %USERPROFILE%\\.cache\\logs\\scripts. Actual: {logDir}"
        );
    }
}
