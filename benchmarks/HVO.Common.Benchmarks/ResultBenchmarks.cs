using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using HVO.Core.Results;

namespace HVO.Common.Benchmarks
{
    [MemoryDiagnoser]
    [SimpleJob(RuntimeMoniker.Net80, launchCount: 1, warmupCount: 3, iterationCount: 5)]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    [CategoriesColumn]
    [BenchmarkCategory("US-019")]
    public class ResultBenchmarks
    {
        private const int OperationsPerInvoke = 1000;
        private readonly Consumer _consumer = new Consumer();
        private readonly Exception _exception = new InvalidOperationException("bench");

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [BenchmarkCategory("Acceptance")]
        public void Success_Create()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                var result = Result<int>.Success(i);
                _consumer.Consume(result);
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [BenchmarkCategory("Acceptance")]
        public void Failure_Create()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                var result = Result<int>.Failure(_exception);
                _consumer.Consume(result);
            }
        }

        [Benchmark]
        [BenchmarkCategory("Logic")]
        public int Match_Success()
        {
            var result = Result<int>.Success(42);
            return result.Match(value => value + 1, _ => 0);
        }

        [Benchmark]
        [BenchmarkCategory("Logic")]
        public int Match_Failure()
        {
            var result = Result<int>.Failure(_exception);
            return result.Match(_ => 1, _ => 0);
        }
    }
}
