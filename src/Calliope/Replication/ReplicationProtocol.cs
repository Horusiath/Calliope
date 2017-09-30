#region copyright
// -----------------------------------------------------------------------
//  <copyright file="ReplicationProtocol.cs" creator="Bartosz Sypytkowski">
//      Copyright (C) 2017 Bartosz Sypytkowski <b.sypytkowski@gmail.com>
//  </copyright>
// -----------------------------------------------------------------------
#endregion

using System;

namespace Calliope.Replication
{
    public interface IReplicationMessage { }
    public interface IReplicationRequest<TMessage> : IReplicationMessage { }
    public interface IReplicationResponse : IReplicationMessage { }

    public sealed class Deliver<TMessage> : IReplicationRequest<TMessage>
    {
        public long SequenceNr { get; }
        public TMessage Message { get; }

        public Deliver(long sequenceNr, TMessage message)
        {
            SequenceNr = sequenceNr;
            Message = message;
        }
    }

    public sealed class DeliverySuccess : IReplicationResponse
    {
        public DeliverySuccess(long sequenceNr)
        {
            SequenceNr = sequenceNr;
        }

        public long SequenceNr { get; }
    }

    public sealed class DeliveryFailure : IReplicationResponse
    {
        public long SequenceNr { get; }
        public Exception Cause { get; }

        public DeliveryFailure(long sequenceNr, Exception cause)
        {
            SequenceNr = sequenceNr;
            Cause = cause;
        }
    }
}