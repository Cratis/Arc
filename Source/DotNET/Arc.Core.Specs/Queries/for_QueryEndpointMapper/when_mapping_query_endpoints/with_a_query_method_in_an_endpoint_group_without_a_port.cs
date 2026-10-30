// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EndpointGroups;

namespace Cratis.Arc.Queries.for_QueryEndpointMapper.when_mapping_query_endpoints;

public class with_a_query_method_in_an_endpoint_group_without_a_port : given.a_query_endpoint_mapper
{
    Exception? _error;

    void Establish()
    {
        var performer = Substitute.For<IQueryPerformer>();
        performer.Type.Returns(typeof(Model));
        performer.Name.Returns(new QueryName(nameof(Model.Query)));
        performer.Location.Returns(["App", "Queries"]);
        _queryPerformerProviders.Performers.Returns([performer]);
    }

    void Because() => _error = Catch.Exception(() => _mapper.MapQueryEndpoints(_serviceProvider));

    [Fact] void should_fail_closed() => _error.ShouldBeOfExactType<EndpointGroupHasNoPort>();

    public record Model
    {
        [EndpointGroup("internal")]
        public static int Query() => 0;
    }
}
