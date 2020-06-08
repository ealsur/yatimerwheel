using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace TimerWheelPerformance
{
    [MemoryDiagnoser]
    public class NormalTimerBenchmark
    {
        private readonly IReadOnlyList<int> timeouts;
        public NormalTimerBenchmark()
        {
            this.timeouts = TimerUtilities.GenerateTimeoutList(10000, 1000, 50);
        }

        [Benchmark]
        public async Task StartAndWait()
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
    }

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