using System;

namespace Calliope.Persistence
{
    /// <summary>
    /// Settings used to configure <see cref="EventStream"/>.
    /// </summary>
    public class EventStreamSettings
    {
        /// <summary>
        /// Maximum number of events replayed in one batch.
        /// </summary>
        public int ReplayBatchCount { get; }

        /// <summary>
        /// Maximum number of replay retries before event stream replay will fail.
        /// </summary>
        public int MaxReplayRetries { get; }

        /// <summary>
        /// Time between two consecutive replay retries.
        /// </summary>
        public TimeSpan ReplayRetryDelay { get; }
    }
}