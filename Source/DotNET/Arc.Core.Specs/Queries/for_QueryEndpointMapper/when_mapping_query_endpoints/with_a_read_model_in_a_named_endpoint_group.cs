// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryEndpointMapper.when_mapping_query_endpoints;

public class with_a_read_model_in_a_named_endpoint_group : given.a_query_endpoint_mapper
{
    void Establish()
    {
        var performer = Substitute.For<IQueryPerformer>();
        performer.Type.Returns(typeof(InternalQueries));
        performer.ReadModelType.Returns(typeof(InternalQueries));
        performer.Name.Returns(new QueryName(nameof(InternalQueries.Get)));
        performer.FullyQualifiedName.Returns(new FullyQualifiedQueryName("App.Queries.Get"));
        performer.Location.Returns(["App", "Queries"]);
        _queryPerformerProviders.Performers.Returns([performer]);
        _arcOptions.EndpointGroups.Assign("internal", 5001);
    }

    void Because() => _mapper.MapQueryEndpoints(_serviceProvider);

    [Fact] void should_map_the_query() => _mapper.Mapped.Count.ShouldEqual(2);
    [Fact] void should_place_every_route_in_the_group() => _mapper.Mapped.TrueForAll(endpoint => endpoint.Metadata?.EndpointGroup == "internal").ShouldBeTrue();
    [Fact] void should_exclude_every_api_description() => _mapper.Mapped.TrueForAll(endpoint => endpoint.Metadata?.ExcludeFromApiDescription == true).ShouldBeTrue();

    [EndpointGroup("internal")]
    public static class InternalQueries
    {
        public static int Get() => 1;
    }
}
