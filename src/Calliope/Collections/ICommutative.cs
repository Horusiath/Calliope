#region copyright
// -----------------------------------------------------------------------
//  <copyright file="ICommutative.cs" creator="Bartosz Sypytkowski">
//      Copyright (C) 2017 Bartosz Sypytkowski <b.sypytkowski@gmail.com>
//  </copyright>
// -----------------------------------------------------------------------
#endregion
namespace Calliope.Collections
{
    /// <summary>
    /// A common interface for all pure operation-based data types.
    /// </summary>
    public interface ICommutative
    {
        
    }

    public interface ICommutativeContainer<TCommutative, TOp, out TResult>
        where TCommutative : ICommutative
    {
        TResult GetValue(TCommutative crdt);
        (TCommutative crdt, TOp operation) Prepare();
        TCommutative Effect(TCommutative, TOp operation);
    }
}