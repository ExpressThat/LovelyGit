namespace LovelyGit.Tests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class PerformanceTestCollection : ICollectionFixture<PerformancePrewarmFixture>
{
    public const string Name = "Performance tests";
}

public sealed class PerformancePrewarmFixture
{
    public PerformancePrewarmFixture() => Git.PerformanceTemplatePrewarmer.WaitForCompletion();
}
