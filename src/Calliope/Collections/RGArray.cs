#region copyright
// -----------------------------------------------------------------------
//  <copyright file="RGArray.cs" creator="Bartosz Sypytkowski">
//      Copyright (C) 2017 Bartosz Sypytkowski <b.sypytkowski@gmail.com>
//  </copyright>
// -----------------------------------------------------------------------
#endregion

using System;
using System.Collections.Immutable;
using System.Linq;

namespace Calliope.Collections
{
    using ReplicaId = Int32;

    /// <summary>
    /// Replicated Growable Array.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RGArray<T> : ICommutative
    {
        #region operations
        
        internal interface IOp { }
        internal sealed class InsertRightOp : IOp
        {
            public Position After { get; }
            public Position CurrentPosition { get; }
            public T Value { get; }

            public InsertRightOp(Position after, Position currentPosition, T value)
            {
                After = after;
                CurrentPosition = currentPosition;
                Value = value;
            }
        }
        internal sealed class DeleteOp : IOp
        {
            public Position At { get; }

            public DeleteOp(Position at)
            {
                At = at;
            }
        }

        #endregion

        private readonly ImmutableList<Vertex<T>> vertices;

        //TODO: optimize
        public ImmutableList<T> Value => vertices.Where(v => !v.IsDeleted).Select(v => v.Value).ToImmutableList();

        public RGArray<T> InsertRight(int index, T value)
        {
            throw new NotImplementedException();
        }

        public RGArray<T> DeleteAt(int index)
        {
            throw new NotImplementedException();
        }

        public RGArray<T> Prune()
        {
            throw new NotImplementedException();
        }
    }

    internal struct Position
    {
        public readonly int Id;
        public readonly ReplicaId ReplicaId;

        public Position(int id, int replicaId)
        {
            Id = id;
            ReplicaId = replicaId;
        }
    }

    internal struct Vertex<T>
    {
        public bool IsDeleted { get; }
        public Position Position { get; }
        public T Value { get; }

        public Vertex(Position position, T value, bool isDeleted = false)
        {
            IsDeleted = isDeleted;
            Position = position;
            Value = value;
        }
    }
}