using System.Collections.Immutable;
using Calliope.Replication.Notifications;
using FluentAssertions;
using Xunit;

namespace Calliope.Tests.Replication
{
    public class RegistryTests
    {
        [Fact]
        public void Registry_should_add_multiple_values_under_given_key()
        {
            var registry = new Registry<string, int>()
                .Add("a", 1)
                .Add("a", 2)
                .Add("b", 3);

            registry["a"].Should().Equal(ImmutableHashSet.CreateRange(new[] { 1, 2 }));
            registry["b"].Should().Equal(ImmutableHashSet.CreateRange(new[] { 3 }));

            registry[1].Should().Be("a");
            registry[2].Should().Be("a");
            registry[3].Should().Be("b");
        }

        [Fact]
        public void Registry_should_remove_multiple_values_from_a_given_key()
        {
            var registry = new Registry<string, int>()
                .Add("a", 1)
                .Add("a", 2)
                .Add("b", 3)
                .Remove("a", 1)
                .Remove("b", 1);

            registry["a"].Should().Equal(ImmutableHashSet.Create(2));
            registry["b"].Should().Equal(ImmutableHashSet.Create(3));
            registry[1].Should().Be(null);
            registry[2].Should().Be("a");
            registry[3].Should().Be("b");

            registry = registry.Remove("b", 3);
            registry["b"].Should().Equal(ImmutableHashSet<int>.Empty);
            registry[3].Should().Be(null);
        }
    }
}