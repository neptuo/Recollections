using Neptuo.Recollections.Tests.TestData.Images;
using Xunit;

namespace Neptuo.Recollections.Tests;

/// <summary>
/// Generates synthetic test fixtures before any tests run.
/// </summary>
public class TestFixtureInitializer : IAsyncLifetime
{
    public Task InitializeAsync()
    {
        EnsureSyntheticExifImage();
        return Task.CompletedTask;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static void EnsureSyntheticExifImage()
    {
        var targetDir = Path.Combine(AppContext.BaseDirectory, "TestData", "Images");
        Directory.CreateDirectory(targetDir);

        var targetPath = Path.Combine(targetDir, "synthetic-exif-gps.jpg");
        
        // Only generate if it doesn't exist (avoids regenerating on every test run)
        if (!File.Exists(targetPath))
        {
            SyntheticExifImageGenerator.GenerateFixture(
                targetPath,
                latitude: 10.5,
                longitude: 20.75,
                altitude: 150.0);
        }
    }
}

/// <summary>
/// Collection definition that runs fixture initialization once per test session.
/// </summary>
[CollectionDefinition(nameof(TestFixtureCollection))]
public class TestFixtureCollection : ICollectionFixture<TestFixtureInitializer>
{
}
