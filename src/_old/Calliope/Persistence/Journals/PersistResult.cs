using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Calliope.Persistence.Journals
{
    public sealed class PersistResult<TEvent>
    {
        private readonly ImmutableArray<Versioned<TEvent>> versions;

        internal PersistResult(ImmutableArray<Versioned<TEvent>> versions)
        {
            this.versions = versions;
        }

        public IEnumerable<Versioned<TEvent>> ConcurrentVersions => versions;
        public bool ConcurrentUpdateDetected => versions.Length > 1;
    }
}