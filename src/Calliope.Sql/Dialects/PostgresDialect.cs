#region copyright
// -----------------------------------------------------------------------
//  <copyright file="PostgresDialect.cs" creator="Bartosz Sypytkowski">
//      Copyright (C) 2017 Bartosz Sypytkowski <b.sypytkowski@gmail.com>
//  </copyright>
// -----------------------------------------------------------------------
#endregion
namespace Calliope.Sql.Dialects
{
    public class PostgresDialect<TRow> : AbstractSqlDialect<TRow> where TRow : IEventRow
    {
        
    }
}