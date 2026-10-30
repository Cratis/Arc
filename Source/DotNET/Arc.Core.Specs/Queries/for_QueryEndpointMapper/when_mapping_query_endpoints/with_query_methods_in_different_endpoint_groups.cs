// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryEndpointMapper.when_mapping_query_endpoints;

public class with_query_methods_in_different_endpoint_groups : given.a_query_endpoint_mapper
{
    void Establish()
    {
        var performers = new[] { Performer(nameof(Model.Public)), Performer(nameof(Model.Internal)) };
        _queryPerformerProviders.Performers.Returns(performers);
        _arcOptions.EndpointGroups.Assign("internal", 5001);
    }

    void Because() => _mapper.MapQueryEndpoints(_serviceProvider);

    [Fact] void should_map_both_queries_over_both_transports() => _mapper.Mapped.Count.ShouldEqual(4);
    [Fact] void should_group_only_the_internal_query() => _mapper.Mapped.Where(e => e.Pattern.EndsWith("/internal", StringComparison.Ordinal)).All(e => e.Metadata?.EndpointGroup == "internal" && e.Metadata.ExcludeFromApiDescription).ShouldBeTrue();
    [Fact] void should_leave_the_public_query_in_the_default_group() => _mapper.Mapped.Where(e => e.Pattern.EndsWith("/public", StringComparison.Ordinal)).All(e => e.Metadata?.EndpointGroup is null).ShouldBeTrue();
    [Fact] void should_leave_the_public_get_query_discoverable() => _mapper.Mapped.Single(e => e.HttpMethod == "GET" && e.Pattern.EndsWith("/public", StringComparison.Ordinal)).Metadata!.ExcludeFromApiDescription.ShouldBeFalse();

    static IQueryPerformer Performer(string name)
    {
        var performer = Substitute.For<IQueryPerformer>();
        performer.Type.Returns(typeof(Model));
        performer.ReadModelType.Returns(typeof(Model));
        performer.Name.Returns(new QueryName(name));
        performer.FullyQualifiedName.Returns(new FullyQualifiedQueryName($"App.Queries.{name}"));
        performer.Location.Returns(["App", "Queries"]);
        return performer;
    }

    public record Model
    {
        public static int Public() => 0;

        [EndpointGroup("internal")]
        public static int Internal() => 0;
    }
}
