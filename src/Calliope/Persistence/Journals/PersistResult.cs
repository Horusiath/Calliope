using System.Collections;
using System.Collections.Generic;

namespace Calliope.Persistence.Journals
{
    public sealed class PersistResult<TEvent>
    {
        public ConcurrentVersions<TEvent> ConcurrentVersions { get; }
        public bool ConcurrentUpdateDetected => ConcurrentVersions.Count > 1;

    }
}