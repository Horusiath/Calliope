#region copyright
// -----------------------------------------------------------------------
//  <copyright file="SqlServerDialect.cs" creator="Bartosz Sypytkowski">
//      Copyright (C) 2017 Bartosz Sypytkowski <b.sypytkowski@gmail.com>
//  </copyright>
// -----------------------------------------------------------------------
#endregion
namespace Calliope.Sql.Dialects
{
    public class SqlServerDialect<TRow> : AbstractSqlDialect<TRow> where TRow : IEventRow
    {
        
    }
}