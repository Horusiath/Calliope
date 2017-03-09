using System.Collections.Generic;
using Calliope.Versioning;
using FluentAssertions;
using Xunit;

namespace Calliope.Tests
{
    public class VectorTimeTests
    {
        [Fact]
        public void VectorTime_should_be_mergeable()
        {
            var t1 = new VectorTime(new KeyValuePair<string, long>("a", 1), new KeyValuePair<string, long>("b", 2), new KeyValuePair<string, long>("c", 2));
            var t2 = new VectorTime(new KeyValuePair<string, long>("a", 4), new KeyValuePair<string, long>("c", 1));

            t1.Merge(t2).Should().Be(new VectorTime(new KeyValuePair<string, long>("a", 4), new KeyValuePair<string, long>("b", 2), new KeyValuePair<string, long>("c", 2)));
            t2.Merge(t1).Should().Be(new VectorTime(new KeyValuePair<string, long>("a", 4), new KeyValuePair<string, long>("b", 2), new KeyValuePair<string, long>("c", 2)));
        }

        [Fact]
        public void VectorTime_should_conform_to_partial_ordering()
        {
            var t1 = new VectorTime(new KeyValuePair<string, long>("a", 1), new KeyValuePair<string, long>("b", 2));
            var t2 = new VectorTime(new KeyValuePair<string, long>("a", 1), new KeyValuePair<string, long>("b", 1));
            var t3 = new VectorTime(new KeyValuePair<string, long>("a", 2), new KeyValuePair<string, long>("b", 1));
            var t4 = new VectorTime(new KeyValuePair<string, long>("a", 1), new KeyValuePair<string, long>("b", 2), new KeyValuePair<string, long>("c", 2));
            var t5 = new VectorTime(new KeyValuePair<string, long>("a", 1), new KeyValuePair<string, long>("c", 2));
            var t6 = new VectorTime(new KeyValuePair<string, long>("a", 1), new KeyValuePair<string, long>("c", 0));

            Assert(t1, t1, eq: true, conc: false, lt: false, lteq: true, gt: false, gteq: true);
            Assert(t1, t2, eq: false, conc: false, lt: false, lteq: false, gt: true, gteq: true);
            Assert(t2, t1, eq: false, conc: false, lt: true, lteq: true, gt: false, gteq: false);
            Assert(t1, t3, eq: false, conc: true, lt: false, lteq: false, gt: false, gteq: false);
            Assert(t3, t1, eq: false, conc: true, lt: false, lteq: false, gt: false, gteq: false);
            Assert(t1, t4, eq: false, conc: false, lt: true, lteq: true, gt: false, gteq: false);
            Assert(t4, t1, eq: false, conc: false, lt: false, lteq: false, gt: true, gteq: true);
            Assert(t1, t5, eq: false, conc: true, lt: false, lteq: false, gt: false, gteq: false);
            Assert(t5, t1, eq: false, conc: true, lt: false, lteq: false, gt: false, gteq: false);
            Assert(t1, t6, eq: false, conc: false, lt: false, lteq: false, gt: true, gteq: true);
            Assert(t6, t1, eq: false, conc: false, lt: true, lteq: true, gt: false, gteq: false);
        }

        private void Assert(VectorTime t1, VectorTime t2, bool eq, bool conc, bool lt, bool lteq, bool gt, bool gteq)
        {
            (t1 == t2).Should().Be(eq, "{0} should be equal to {1}", t1, t2);
            (t1.IsConcurrent(t2)).Should().Be(conc, "{0} should be concurrent to {1}", t1, t2);
            (t1 < t2).Should().Be(lt, "{0} should be less than {1}", t1, t2);
            (t1 <= t2).Should().Be(lteq, "{0} should be less or equal to {1}", t1, t2);
            (t1 > t2).Should().Be(gt, "{0} should be greater than {1}", t1, t2);
            (t1 >= t2).Should().Be(gteq, "{0} should be greater or equal to {1}", t1, t2);
        }
    }
}