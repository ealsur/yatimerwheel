using System;

namespace TimerWheel
{
    public abstract class TimerWheel : IDisposable
    {
        public abstract void Dispose();

        public abstract TimerWheelTimer GetTimer(int timeoutInMs);

        public abstract void SubscribeForTimeouts(TimerWheelTimer timer);

        public static TimerWheel CreateTimerWheel(
            int resolutionInMs,
            int buckets)
        {
            return new TimerWheelCore(resolutionInMs, buckets);   
        }
    }
}