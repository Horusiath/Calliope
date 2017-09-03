using System.Collections;
using System.Collections.Generic;

namespace Calliope.Replication
{
    public interface ITransactional { }

    internal sealed class AtomicWrite : ITransactional, IPartiallyComparable<AtomicWrite>
    {
        public int? PartiallyCompareTo(AtomicWrite other)
        {
            throw new System.NotImplementedException();
        }
    }

    internal sealed class AtomicRead : ITransactional, IPartiallyComparable<AtomicRead>
    {
        public int? PartiallyCompareTo(AtomicRead other)
        {
            throw new System.NotImplementedException();
        }
    }
}