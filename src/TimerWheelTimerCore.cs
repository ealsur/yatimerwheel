using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("TimerWheelTests")]
namespace SimpleTimerWheel
{
    internal sealed class TimerWheelTimerCore : TimerWheelTimer
    {
        private readonly TaskCompletionSource<object> taskCompletionSource;
        private readonly Object memberLock;
        private readonly TimerWheel timerWheel;
        private bool timerStarted = false;

        internal TimerWheelTimerCore(
            TimeSpan timeoutPeriod, 
            TimerWheel timerWheel)
        {
            if (timeoutPeriod.Ticks == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(timeoutPeriod));
            }

            this.timerWheel = timerWheel ?? throw new ArgumentNullException(nameof(timerWheel));
            this.Timeout = timeoutPeriod;
            this.taskCompletionSource = new TaskCompletionSource<object>();
            this.memberLock = new Object();
        }

        public override TimeSpan Timeout { get; }

        public override Task StartTimerAsync()
        {
            lock (this.memberLock)
            {
                if (this.timerStarted)
                {
                    // use only once enforcement
                    throw new InvalidOperationException("Timer Already Started");
                }

                this.timerWheel.SubscribeForTimeouts(this);
                this.timerStarted = true;
                return this.taskCompletionSource.Task;
            }
        }

        public override bool CancelTimer()
        {
            return this.taskCompletionSource.TrySetCanceled();
        }

        public override bool FireTimeout()
        {
            return this.taskCompletionSource.TrySetResult(null);
        }
    }
}