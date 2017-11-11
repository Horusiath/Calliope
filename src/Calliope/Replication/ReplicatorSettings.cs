#region copyright
// -----------------------------------------------------------------------
//  <copyright file="ReplicatorSettings.cs" creator="Bartosz Sypytkowski">
//      Copyright (C) 2017 Bartosz Sypytkowski <b.sypytkowski@gmail.com>
//  </copyright>
// -----------------------------------------------------------------------
#endregion

using System;

namespace Calliope.Replication
{
    /// <summary>
    /// Settings used by the <see cref="ReplicatorActorActor{T}"/> actor.
    /// </summary>
    public class ReplicatorSettings
    {
        public static ReplicatorSettings Default { get; } = new ReplicatorSettings(
            resendInterval: TimeSpan.FromSeconds(5),
            retryTimeout: TimeSpan.FromSeconds(10),
            role: null);

        /// <summary>
        /// Time interval, in which replicator will try to redeliver messages, that
        /// have not been acknowledged.
        /// </summary>
        public TimeSpan ResendInterval { get; }

        /// <summary>
        /// Each replicator must acknowledge received message send unter provided
        /// timeout, specified by this value.
        /// </summary>
        public TimeSpan RetryTimeout { get; }

        /// <summary>
        /// Role used to recognize cluster nodes, which have a replicator capabiities.
        /// </summary>
        public string Role { get; }

        public ReplicatorSettings(
            TimeSpan resendInterval,
            TimeSpan retryTimeout,
            string role)
        {
            ResendInterval = resendInterval;
            RetryTimeout = retryTimeout;
            Role = role;
        }

        public ReplicatorSettings WithRole(string role)
        {
            if (string.IsNullOrEmpty(role)) throw new ArgumentNullException(nameof(role));

            return new ReplicatorSettings(ResendInterval, RetryTimeout, role);
        }

        public ReplicatorSettings WithRetryTimeout(TimeSpan retryTimeout)
        {
            return new ReplicatorSettings(ResendInterval, retryTimeout, Role);
        }

        public ReplicatorSettings WithResendInterval(TimeSpan resendInterval)
        {
            return new ReplicatorSettings(resendInterval, RetryTimeout, Role);
        }
    }
}