#region copyright
// -----------------------------------------------------------------------
//  <copyright file="ORSet.cs" creator="Bartosz Sypytkowski">
//      Copyright (C) 2017 Bartosz Sypytkowski <b.sypytkowski@gmail.com>
//  </copyright>
// -----------------------------------------------------------------------
#endregion
namespace Calliope.Collections
{
    /// <summary>
    /// Observed Remove Set.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ORSet<T> : ICommutative
    {
        #region operations

        internal enum OpType
        {
            Add = 1,
            Remove = 2
        }

        internal struct UpdateOp
        {
            public OpType Type { get; }
            public T Value { get; }

            public UpdateOp(OpType type, T value)
            {
                Type = type;
                Value = value;
            }

            public static UpdateOp Add(T value) => new UpdateOp(OpType.Add, value);
            public static UpdateOp Remove(T value) => new UpdateOp(OpType.Remove, value);
        }

        #endregion
    }

    /// <summary>
    /// Add-wins Observed Remove Set.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class AWORSet<T> : ORSet<T>
    {
        
    }
}