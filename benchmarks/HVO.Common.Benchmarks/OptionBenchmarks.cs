using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using HVO.Core.Options;

namespace HVO.Common.Benchmarks
{
    [MemoryDiagnoser]
    [SimpleJob(RuntimeMoniker.Net80, launchCount: 1, warmupCount: 3, iterationCount: 5)]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    [CategoriesColumn]
    [BenchmarkCategory("US-019")]
    public class OptionBenchmarks
    {
        private const int OperationsPerInvoke = 1000;
        private readonly Consumer _consumer = new Consumer();

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [BenchmarkCategory("Acceptance")]
        public void Some_Create()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                var option = new Option<string>("value");
                _consumer.Consume(option);
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [BenchmarkCategory("Acceptance")]
        public void None_Create()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                var option = Option<string>.None();
                _consumer.Consume(option);
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [BenchmarkCategory("Logic")]
        public void GetValueOrDefault()
        {
            var option = new Option<string>("value");
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                var value = option.GetValueOrDefault("default");
                _consumer.Consume(value);
            }
        }
    }
}
