// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;

namespace Cratis.Arc.Queries.for_QueryEndpointMapper.when_mapping_query_endpoints;

public class with_two_performers_having_same_method_name_in_different_namespaces : given.a_query_endpoint_mapper
{
    IQueryPerformer _firstPerformer;
    IQueryPerformer _secondPerformer;

    void Establish()
    {
        _firstPerformer = Substitute.For<IQueryPerformer>();
        _firstPerformer.Name.Returns(new QueryName("GetAll"));
        _firstPerformer.FullyQualifiedName.Returns(new FullyQualifiedQueryName("Namespace1.Feature1.ReadModel1.GetAll"));
        _firstPerformer.Location.Returns(["Namespace1", "Feature1", "ReadModel1"]);
        _firstPerformer.AllowsAnonymousAccess.Returns(false);
        _firstPerformer.Parameters.Returns(new QueryParameters([]));

        _secondPerformer = Substitute.For<IQueryPerformer>();
        _secondPerformer.Name.Returns(new QueryName("GetAll"));
        _secondPerformer.FullyQualifiedName.Returns(new FullyQualifiedQueryName("Namespace2.Feature2.ReadModel2.GetAll"));
        _secondPerformer.Location.Returns(["Namespace2", "Feature2", "ReadModel2"]);
        _secondPerformer.AllowsAnonymousAccess.Returns(false);
        _secondPerformer.Parameters.Returns(new QueryParameters([]));

        _queryPerformerProviders.Performers.Returns([_firstPerformer, _secondPerformer]);

        _mapper.EndpointExists(Arg.Any<string>()).Returns(false);
    }

    void Because() => _mapper.MapQueryEndpoints(_serviceProvider);

    [Fact] void should_map_two_get_endpoints() => _mapper.Received(2).MapGet(
        Arg.Any<string>(),
        Arg.Any<Func<IHttpRequestContext, Task>>(),
        Arg.Any<EndpointMetadata>());
}
