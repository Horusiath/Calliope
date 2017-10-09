#region copyright
// -----------------------------------------------------------------------
//  <copyright file="Counter.cs" creator="Bartosz Sypytkowski">
//      Copyright (C) 2017 Bartosz Sypytkowski <b.sypytkowski@gmail.com>
//  </copyright>
// -----------------------------------------------------------------------
#endregion

using System;

namespace Calliope.Collections
{
    /// <summary>
    /// Incrementable/decrementable counter.
    /// </summary>
    public sealed class Counter : ICommutative
    {
        #region operations

        internal struct UpdateOp
        {
            public UpdateOp(long value)
            {
                Value = value;
            }

            public long Value { get; }
        }

        #endregion

        public Counter(long value)
        {
            Value = value;
        }

        public long Value { get; }

        public Counter Update(long delta)
        {
            throw new NotImplementedException();
        }

        public static Counter operator +(Counter x, long value) => x.Update(value);
        public static Counter operator -(Counter x, long value) => x.Update(-value);
        public static implicit operator Counter(long value) => new Counter(value);
        public static implicit operator long(Counter x) => x.Value;
    }
}