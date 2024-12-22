using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;

[Config(typeof(Config))]
public class Bench
{
    private class Config : ManualConfig
    {
        public Config()
        {
            var job = Job.Default
                .WithStrategy(RunStrategy.Monitoring) // only monitoring
                .WithWarmupCount(3) // light warm-up
                .WithIterationCount(10) // only 10 interations
                .WithLaunchCount(1); // launch test only once

            // add the jobs for all versions:
            AddJob(
                job.WithNuGet("Lib", "0.1.0-alpha1").WithId("alpha1").WithBaseline(true),
                job.WithNuGet("Lib", "0.1.0-alpha2").WithId("alpha2"),
                job.WithNuGet("Lib", "0.1.0-alpha3").WithId("alpha3"),
                job.WithNuGet("Lib", "0.1.0-beta").WithId("beta")
            );

            // add percentage column
            SummaryStyle =
                SummaryStyle.Default
                .WithRatioStyle(BenchmarkDotNet.Columns.RatioStyle.Percentage);
        }
    }

    [Benchmark]
    public void SomethingUsefull()
    {
        new Lib.Class().SomethingUsefull();
    }
}