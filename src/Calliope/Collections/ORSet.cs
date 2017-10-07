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
        
    }

    /// <summary>
    /// Add-wins Observed Remove Set.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class AWORSet<T> : ORSet<T>
    {
        
    }
}