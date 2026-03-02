using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using HVO.Core.OneOf;

namespace HVO.Common.Benchmarks
{
    [MemoryDiagnoser]
    [SimpleJob(RuntimeMoniker.Net80, launchCount: 1, warmupCount: 3, iterationCount: 5)]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    [CategoriesColumn]
    [BenchmarkCategory("US-019")]
    public class OneOfBenchmarks
    {
        private const int OperationsPerInvoke = 1000;
        private readonly Consumer _consumer = new Consumer();

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [BenchmarkCategory("Acceptance")]
        public void Create_FromT1()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                var value = OneOf<int, string>.FromT1(i);
                _consumer.Consume(value);
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [BenchmarkCategory("Acceptance")]
        public void Create_FromT2()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                var value = OneOf<int, string>.FromT2("value");
                _consumer.Consume(value);
            }
        }

        [Benchmark]
        [BenchmarkCategory("Logic")]
        public int Match_T1()
        {
            var value = OneOf<int, string>.FromT1(42);
            return value.Match(v => v, _ => 0);
        }

        [Benchmark]
        [BenchmarkCategory("Logic")]
        public int Match_T2()
        {
            var value = OneOf<int, string>.FromT2("value");
            return value.Match(_ => 0, v => v.Length);
        }
    }
}
