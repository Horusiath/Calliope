using System;
using System.Runtime.Serialization;
using Akka.Actor;

namespace Calliope
{
    [Serializable]
    public class PersistenceException : AkkaException
    {
        public PersistenceException() { }
        public PersistenceException(string message) : base(message) { }
        public PersistenceException(string message, Exception inner) : base(message, inner) { }
        protected PersistenceException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    [Serializable]
    public class SaveSnapshotException : PersistenceException
    {
        public SaveSnapshotException(string message) : base(message) { }
        protected SaveSnapshotException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    [Serializable]
    public class CorrelationException : AkkaException
    {
        public int CorrelationId { get; }

        public CorrelationException(string message, int correlationId) : base(message)
        {
            CorrelationId = correlationId;
        }

        protected CorrelationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}