using System.Collections;
using System.Collections.Generic;

namespace Calliope
{
    public class ConcurrentVersions<T> : IEnumerable<T>
    {
        public int Count { get; set; }
    }
}