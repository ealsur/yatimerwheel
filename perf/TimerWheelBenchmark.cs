using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using SimpleTimerWheel;

namespace TimerWheelPerformance
{
    [Config(typeof(Config))]
    [MemoryDiagnoser]
    public class TimerWheelBenchmark
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                this.AddJob(Job.MediumRun.WithGcServer(true).WithGcForce(false).WithId("Server"));
            }
        }

        private readonly TimerWheel mainWheel;
        private readonly IReadOnlyList<int> timeouts;
        public TimerWheelBenchmark()
        {
            this.timeouts = TimerUtilities.GenerateTimeoutList(10000, 1000, 50);
            this.mainWheel = TimerWheel.CreateTimerWheel(TimeSpan.FromMilliseconds(50), 20);
        }

        [Benchmark]
        public async Task TenK_WithTimerWheel()
        {
            TimerWheel wheel = TimerWheel.CreateTimerWheel(TimeSpan.FromMilliseconds(50), 20);
            List<Task> timers = new List<Task>(this.timeouts.Count);
            for (int i = 0; i < this.timeouts.Count; i++)
            {
                SimpleTimerWheel.TimerWheelTimer timer = wheel.CreateTimer(TimeSpan.FromMilliseconds(this.timeouts[i]));
                timers.Add(timer.StartTimerAsync());
            }

            await Task.WhenAll(timers);
            wheel.Dispose();
        }

        [Benchmark]
        public async Task TenK_WithNormalTimers()
        {
            List<Task> timers = new List<Task>(this.timeouts.Count);
            List<WorkerWithTimer> workers = new List<WorkerWithTimer>(this.timeouts.Count);
            for (int i = 0; i < this.timeouts.Count; i++)
            {
                WorkerWithTimer timer = new WorkerWithTimer(this.timeouts[i]);
                timers.Add(timer.StartTimerAsync());
            }

            await Task.WhenAll(timers);
            foreach (WorkerWithTimer item in workers)
            {
                item.Dispose();
            }
        }

        [Benchmark]
        public async Task One_WithTimerWheel()
        {
            TimerWheelTimer timer = this.mainWheel.CreateTimer(TimeSpan.FromMilliseconds(50));
            await timer.StartTimerAsync();
        }

        [Benchmark]
        public async Task One_WithNormalTimers()
        {
            WorkerWithTimer timer = new WorkerWithTimer(50);
            await timer.StartTimerAsync();
        }

        public void DoNothing(Object state){}

        public class WorkerWithTimer: IDisposable
        {
            private readonly TaskCompletionSource<object> taskCompletionSource;
            private readonly int timeout;
            private Timer timer;
            public WorkerWithTimer(int timeout)
            {
                this.timeout = timeout;
                this.taskCompletionSource = new TaskCompletionSource<object>();
            }

            public Task StartTimerAsync()
            {
                this.timer = new Timer(this.OnTimer, null, this.timeout, this.timeout);
                return this.taskCompletionSource.Task;
            }

            public void OnTimer(Object stateInfo)
            {
                this.taskCompletionSource.TrySetResult(null);
            }

            public void Dispose()
            {
                this.timer.Dispose();
            }
        }
    }
}