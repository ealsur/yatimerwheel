using System;
using System.Threading.Tasks;

namespace SimpleTimerWheel
{
    public abstract class TimerWheelTimer
    {
        /// <summary>
        /// Timeout of the timer.
        /// </summary>
        public abstract TimeSpan Timeout { get; }

        /// <summary>
        /// Starts the timer based on the Timeout configuration.
        /// </summary>
        public abstract Task StartTimerAsync();

        /// <summary>
        /// Cancels the timer.
        /// </summary>
        public abstract bool CancelTimer();

        /// <summary>
        /// Fire the associated timeout callback.
        /// </summary>
        public abstract bool FireTimeout();
    }
}