// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryEndpointMapper.when_mapping_query_endpoints;

public class with_an_endpoint_group_route_prefix : given.a_query_endpoint_mapper
{
    void Establish()
    {
        _arcOptions.GeneratedApis.RoutePrefix = "api";
        _arcOptions.EndpointGroups.Assign("internal", 5001).SetRoutePrefix("internal", "internal-api");
        var performers = new[] { Performer(typeof(InternalModel), "Internal"), Performer(typeof(PublicModel), "Public") };
        _queryPerformerProviders.Performers.Returns(performers);
    }

    void Because() => _mapper.MapQueryEndpoints(_serviceProvider);

    [Fact] void should_map_both_queries() => _mapper.Mapped.Count.ShouldEqual(4);
    [Fact] void should_use_the_group_prefix() => _mapper.Mapped.Where(e => e.Metadata?.EndpointGroup == "internal").All(e => e.Pattern.StartsWith("/internal-api/", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_keep_the_default_prefix() => _mapper.Mapped.Where(e => e.Metadata?.EndpointGroup is null).All(e => e.Pattern.StartsWith("/api/", StringComparison.Ordinal)).ShouldBeTrue();

    static IQueryPerformer Performer(Type type, string name)
    {
        var performer = Substitute.For<IQueryPerformer>();
        performer.Type.Returns(type);
        performer.ReadModelType.Returns(type);
        performer.Name.Returns(new QueryName(name));
        performer.FullyQualifiedName.Returns(new FullyQualifiedQueryName($"App.Queries.{name}"));
        performer.Location.Returns(["App", "Queries"]);
        return performer;
    }

    [EndpointGroup("internal")]
    public record InternalModel;

    public record PublicModel;
}
