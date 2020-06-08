using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

[assembly: InternalsVisibleTo("TimerWheelTests")]
namespace TimerWheel
{
    internal class TimerWheelCore : TimerWheel, IDisposable
    {
        private readonly ConcurrentDictionary<int, ConcurrentQueue<TimerWheelTimer>> timers;
        private readonly int resolutionInTicks;
        private readonly int resolutionInMs;
        private readonly int buckets;        
        private readonly Timer timer;
        private readonly object subscriptionLock;
        private readonly object timerConcurrencyLock;
        private bool isDisposed = false;
        private bool isRunning = false;
        private int expirationIndex = 0;

        internal TimerWheelCore(
            int resolutionInMs,
            int buckets)
        {
            if (resolutionInMs <= 20)
            {
                throw new ArgumentOutOfRangeException(nameof(resolutionInMs), "Value is too low, machine resolution less than 20 ms has unexpected results https://docs.microsoft.com/dotnet/api/system.threading.timer");
            }

            if (buckets <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(buckets));
            }

            this.resolutionInMs = resolutionInMs;
            this.resolutionInTicks = (int)TimeSpan.FromMilliseconds(resolutionInMs).Ticks;
            this.buckets = buckets;
            this.timers = new ConcurrentDictionary<int, ConcurrentQueue<TimerWheelTimer>>();
            this.subscriptionLock = new object();
            this.timerConcurrencyLock = new object();
            this.timer = new Timer(this.OnTimer, state: null, this.resolutionInMs, this.resolutionInMs);
        }

        /// <summary>
        /// Used only for unit tests.
        /// </summary>
        internal TimerWheelCore(
            int resolutionInMs,
            int buckets,
            Timer timer)
            : this(resolutionInMs, buckets)
        {
            this.timer.Dispose();
            this.timer = timer;
        }

        public override void Dispose()
        {
            if (this.isDisposed)
            {
                return;
            }

            this.DisposeAllTimers();

            this.isDisposed = true;
        }

        public override TimerWheelTimer GetTimer(int timeoutInMs)
        {
            this.ThrowIfDisposed();
            if (timeoutInMs < this.resolutionInMs)
            {
                throw new ArgumentOutOfRangeException(nameof(timeoutInMs), $"TimerWheel configured with {this.resolutionInMs} resolution, cannot use a smaller timeout of {timeoutInMs}.");
            }

            if (timeoutInMs % this.resolutionInMs != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(timeoutInMs), $"TimerWheel configured with {this.resolutionInMs} resolution, cannot use a different resolution of {timeoutInMs}.");
            }

            if (timeoutInMs > this.resolutionInMs * this.buckets)
            {
                throw new ArgumentOutOfRangeException(nameof(timeoutInMs), $"TimerWheel configured with {this.resolutionInMs * this.buckets} max, cannot use a larger timeout of {timeoutInMs}.");
            }

            return new TimerWheelTimerCore(TimeSpan.FromMilliseconds(timeoutInMs), this);
        }

        public override void SubscribeForTimeouts(TimerWheelTimer timer)
        {
            this.ThrowIfDisposed();
            long timerTimeoutInTicks = timer.Timeout.Ticks;
            int bucket = (int)timerTimeoutInTicks / this.resolutionInTicks;
            lock (this.subscriptionLock)
            {
                int index = this.GetIndexForTimeout(bucket);
                ConcurrentQueue<TimerWheelTimer> timerQueue;
                if(this.timers.TryGetValue(index, out timerQueue))
                {
                    timerQueue.Enqueue(timer);
                }
                else
                {
                    timerQueue = this.timers.GetOrAdd(index,
                        delegate
                        {
                            return new ConcurrentQueue<TimerWheelTimer>();
                        });
                    timerQueue.Enqueue(timer);
                }
            }
        }

        public void OnTimer(Object stateInfo)
        {
            lock (this.timerConcurrencyLock)
            {
                if (!this.isRunning)
                {
                    this.isRunning = true;
                }
                else
                {
                    return;
                }
            }

            try
            {
                if (this.timers.TryGetValue(this.expirationIndex, out ConcurrentQueue<TimerWheelTimer> timerQueue))
                {
                    while (timerQueue.TryDequeue(out TimerWheelTimer timer))
                    {
                        timer.FireTimeout();
                    }
                }

                if (++this.expirationIndex == this.buckets)
                {
                    this.expirationIndex = 0;
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("Hit exception ex: {0}\n, stack: {1}", ex.Message, ex.StackTrace);
            }
            finally
            {
                lock (this.timerConcurrencyLock)
                {
                    this.isRunning = false;
                }
            }
        }

        private int GetIndexForTimeout(int bucket)
        {
            int index = bucket + this.expirationIndex;
            if (index > this.buckets)
            {
                index -= this.buckets;
            }

            return index - 1; // zero based
        }

        private void DisposeAllTimers()
        {
            foreach (KeyValuePair<int, ConcurrentQueue<TimerWheelTimer>> kv in this.timers)
            {
                ConcurrentQueue<TimerWheelTimer> pooledTimerQueue = kv.Value;
                while (pooledTimerQueue.TryDequeue(out TimerWheelTimer timer))
                {
                    timer.CancelTimer();
                }
            }

            this.timer?.Dispose();
        }

        private void ThrowIfDisposed()
        {
            if (this.isDisposed)
            {
                throw new ObjectDisposedException("TimerWheel is disposed.");
            }
        }
    }
}