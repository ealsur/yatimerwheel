using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace TimerWheelPerformance
{
    [MemoryDiagnoser]
    public class TimerWheelBenchmark
    {
        private readonly IReadOnlyList<int> timeouts;
        public TimerWheelBenchmark()
        {
            this.timeouts = TimerUtilities.GenerateTimeoutList(10000, 1000, 50);
        }

        [Benchmark]
        public async Task StartAndWait()
        {
            TimerWheel.TimerWheel wheel = TimerWheel.TimerWheel.CreateTimerWheel(50, 20);
            List<Task> timers = new List<Task>(this.timeouts.Count);
            for (int i = 0; i < this.timeouts.Count; i++)
            {
                TimerWheel.TimerWheelTimer timer = wheel.GetTimer(this.timeouts[i]);
                timers.Add(timer.StartTimerAsync());
            }

            await Task.WhenAll(timers);
            wheel.Dispose();
        }
    }
}