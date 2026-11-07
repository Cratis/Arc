// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryEndpointMapper.when_mapping_query_endpoints;

public class with_a_query_whose_location_is_skipped_entirely : given.a_query_endpoint_mapper
{
    void Establish()
    {
        var performer = Substitute.For<IQueryPerformer>();
        performer.Type.Returns(typeof(Queries));
        performer.ReadModelType.Returns(typeof(Queries));
        performer.Name.Returns(new QueryName(nameof(Queries.Get)));
        performer.FullyQualifiedName.Returns(new FullyQualifiedQueryName("Queries.Get"));
        performer.Location.Returns([]);
        _queryPerformerProviders.Performers.Returns([performer]);
    }

    void Because() => _mapper.MapQueryEndpoints(_serviceProvider);

    [Fact] void should_map_the_query() => _mapper.Mapped.Count.ShouldEqual(2);
    [Fact] void should_not_publish_an_empty_tag() => _mapper.Mapped.TrueForAll(endpoint => endpoint.Metadata!.Tags!.All(tag => !string.IsNullOrEmpty(tag))).ShouldBeTrue();
    [Fact] void should_not_publish_any_tag() => _mapper.Mapped.TrueForAll(endpoint => !endpoint.Metadata!.Tags!.Any()).ShouldBeTrue();

    public static class Queries
    {
        public static int Get() => 1;
    }
}
