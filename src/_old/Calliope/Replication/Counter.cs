using System;
using System.Threading.Tasks;

namespace Calliope.Replication
{
    public sealed class Counter : IEquatable<Counter>, IComparable<Counter>
    {
        #region operations

        public struct Add
        {
            public Add(long value)
            {
                Value = value;
            }

            public long Value { get; }
        }

        #endregion

        public static readonly Counter Zero = new Counter();

        public long Value { get; }

        internal Counter(long value = 0L)
        {
            Value = value;
        }

        public bool Equals(Counter other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Value == other.Value;
        }

        public override bool Equals(object obj) => obj is Counter gcounter && Equals(gcounter);

        public override int GetHashCode() => Value.GetHashCode();

        public override string ToString() => $"GCounter({Value})";

        public int CompareTo(Counter other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            return Value.CompareTo(other.Value);
        }

        public static implicit operator long(Counter gcounter) => gcounter.Value;
        public static implicit operator Counter(long value) => new Counter(value);
    }

    internal struct CounterWitness : ICrdtTrait<Counter, long, Counter.Add>
    {
        public Counter Zero => Counter.Zero;

        public long GetValue(Counter counter) => counter.Value;

        public object Prepare(Counter crdt, Counter.Add operation) => operation;

        public Counter Effect(Counter crdt, Counter.Add operation) => new Counter(crdt.Value + operation.Value);
    }

    public sealed class CounterService : ICrdtService<CounterWitness, Counter, long, Counter.Add>
    {
        public async Task<long> Add(long value)
        {
            throw new NotImplementedException();
        }
    }
}