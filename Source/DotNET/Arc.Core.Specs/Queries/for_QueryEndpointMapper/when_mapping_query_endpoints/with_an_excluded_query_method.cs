// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryEndpointMapper.when_mapping_query_endpoints;

public class with_an_excluded_query_method : given.a_query_endpoint_mapper
{
    void Establish()
    {
        var performer = Substitute.For<IQueryPerformer>();
        performer.Type.Returns(typeof(Queries));
        performer.ReadModelType.Returns(typeof(Queries));
        performer.Name.Returns(new QueryName(nameof(Queries.Get)));
        performer.FullyQualifiedName.Returns(new FullyQualifiedQueryName("App.Queries.Get"));
        performer.Location.Returns(["App", "Queries"]);
        _queryPerformerProviders.Performers.Returns([performer]);
    }

    void Because() => _mapper.MapQueryEndpoints(_serviceProvider);

    [Fact] void should_still_map_the_query() => _mapper.Mapped.Count.ShouldEqual(2);
    [Fact] void should_exclude_all_query_api_descriptions() => _mapper.Mapped.TrueForAll(endpoint => endpoint.Metadata?.ExcludeFromApiDescription == true).ShouldBeTrue();

    public static class Queries
    {
        [ExcludeFromDiscovery]
        public static int Get() => 1;
    }
}
