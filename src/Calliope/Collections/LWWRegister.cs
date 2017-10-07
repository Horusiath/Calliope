#region copyright
// -----------------------------------------------------------------------
//  <copyright file="LWWRegister.cs" creator="Bartosz Sypytkowski">
//      Copyright (C) 2017 Bartosz Sypytkowski <b.sypytkowski@gmail.com>
//  </copyright>
// -----------------------------------------------------------------------
#endregion

using System;
using System.Collections.Generic;

namespace Calliope.Collections
{
    /// <summary>
    /// Last Write Wins Register.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class LWWRegister<T> : ICommutative
    {
        #region comparers
        private sealed class OldestWinsComparer : IComparer<LWWRegister<T>>
        {
            public int Compare(LWWRegister<T> x, LWWRegister<T> y)
            {
                throw new NotImplementedException();
            }
        }

        private sealed class YoungestWinsComparer : IComparer<LWWRegister<T>>
        {
            public int Compare(LWWRegister<T> x, LWWRegister<T> y)
            {
                throw new NotImplementedException();
            }
        }
        #endregion

        public DateTime Timestamp { get; }
        public T Value { get; }
    }
}