#region copyright
// -----------------------------------------------------------------------
//  <copyright file="VClockConverter.cs" creator="Bartosz Sypytkowski">
//      Copyright (C) 2017 Bartosz Sypytkowski <b.sypytkowski@gmail.com>
//  </copyright>
// -----------------------------------------------------------------------
#endregion

using System.Data;
using Dapper;

namespace Calliope.Sql.Converters
{
    public sealed class VClockConverter : SqlMapper.TypeHandler<VClock>
    {
        public override void SetValue(IDbDataParameter parameter, VClock value)
        {
            throw new System.NotImplementedException();
        }

        public override VClock Parse(object value)
        {
            throw new System.NotImplementedException();
        }
    }
}