// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryEndpointMapper.when_mapping_query_endpoints;

public class with_query_http_method_disabled : given.a_query_endpoint_mapper
{
    record ReadModel;

    IQueryPerformer _performer;

    void Establish()
    {
        _arcOptions.GeneratedApis.EnableQueryHttpMethod = false;

        _performer = Substitute.For<IQueryPerformer>();
        _performer.Name.Returns(new QueryName("GetAll"));
        _performer.FullyQualifiedName.Returns(new FullyQualifiedQueryName("Namespace.Feature.ReadModel.GetAll"));
        _performer.ReadModelType.Returns(typeof(ReadModel));
        _performer.Location.Returns(["Namespace", "Feature", "ReadModel"]);
        _performer.AllowsAnonymousAccess.Returns(false);
        _performer.Parameters.Returns(new QueryParameters([]));

        _queryPerformerProviders.Performers.Returns([_performer]);
    }

    void Because() => _mapper.MapQueryEndpoints(_serviceProvider);

    [Fact] void should_map_the_get_endpoint() => _mapper.CountFor("GET").ShouldEqual(1);
    [Fact] void should_not_map_a_query_endpoint() => _mapper.CountFor("QUERY").ShouldEqual(0);
}
