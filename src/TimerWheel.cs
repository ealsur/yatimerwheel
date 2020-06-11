using System;

namespace SimpleTimerWheel
{
    /// <summary>
    /// TimerWheel is a Simple TimerWheel implementation that uses a single System.Threading.Timer to maintain a wheel.
    /// Creation of the wheel requires the resolution of each wheel's bucket and the amount of buckets, which define which its MaxInterval.
    /// Timed-based tasks can use <see cref="TimerWheel.CreateTimer(TimeSpan)"/> to obtain a new timer.
    /// Starting the timer through <see cref="TimerWheelTimer.StartTimerAsync()"/> returns a Task that can be awaited and will complete on time expiration.
    /// </summary>
    /// <example>
    /// <code language="c#">
    /// <![CDATA[
    /// TimerWheel wheel = TimerWheel.CreateTimerWheel(resolution: TimeSpan.FromMilliseconds(50), buckets: 20);
    /// TimerWheelTimer timer = wheel.CreateTimer(TimeSpan.FromMilliseconds(100));
    /// await timer.StartTimerAsync();
    /// ]]>
    /// </code>
    /// </example>
    public abstract class TimerWheel : IDisposable
    {
        public abstract void Dispose();

        public abstract TimerWheelTimer CreateTimer(TimeSpan timeout);

        public abstract void SubscribeForTimeouts(TimerWheelTimer timer);

        /// <summary>
        /// Creates a new <see cref="TimerWheel"/> which is a simple timer wheel implementation
        /// </summary>
        /// <remarks>
        /// The <paramref name="resolution"/> defines the minimum supported timeout and <paramref name="buckets"/> times <paramref name="resolution"/> define the maximum supported timeout.
        /// </remarks>
        /// <param name="resolution">Amount of time for each wheel step.</param>
        /// <param name="buckets">Amount of slices in the wheel</param>
        public static TimerWheel CreateTimerWheel(
            TimeSpan resolution,
            int buckets)
        {
            return new TimerWheelCore(resolution, buckets);   
        }
    }
}