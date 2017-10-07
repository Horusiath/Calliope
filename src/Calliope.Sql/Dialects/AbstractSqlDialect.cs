#region copyright
// -----------------------------------------------------------------------
//  <copyright file="AbstractSqlDialect.cs" creator="Bartosz Sypytkowski">
//      Copyright (C) 2017 Bartosz Sypytkowski <b.sypytkowski@gmail.com>
//  </copyright>
// -----------------------------------------------------------------------
#endregion
namespace Calliope.Sql.Dialects
{
    public abstract class AbstractSqlDialect<TRow> : ISqlDialect<TRow> where TRow : IEventRow
    {
        
    }
}