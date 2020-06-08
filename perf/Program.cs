using System;
using BenchmarkDotNet.Running;

namespace TimerWheelPerformance
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<TimerWheelBenchmark>();
        }
    }
}
