// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;

namespace Cratis.Arc.Queries.for_QueryEndpointMapper.when_handling_a_query;

public class with_a_custom_pipeline_that_throws_invalid_authorization_configuration : given.a_query_request
{
    Exception? _error;

    void Establish() => _queryPipeline.Perform(
        Arg.Any<FullyQualifiedQueryName>(),
        Arg.Any<QueryArguments>(),
        Arg.Any<Paging>(),
        Arg.Any<Sorting>(),
        Arg.Any<IServiceProvider>(),
        Arg.Any<CancellationToken>())
        .Returns<Task<QueryResult>>(_ => throw new InvalidAuthorizationConfiguration("Raised by the custom query pipeline."));

    async Task Because() => _error = await Catch.Exception(() => _mapper.HandlerFor("GET")(_context));

    [Fact] void should_not_map_a_pipeline_exception_to_forbidden() => _error.ShouldBeOfExactType<InvalidAuthorizationConfiguration>();
    [Fact] void should_have_called_the_custom_pipeline() => _queryPipeline.Received(1).Perform(
        Arg.Any<FullyQualifiedQueryName>(),
        Arg.Any<QueryArguments>(),
        Arg.Any<Paging>(),
        Arg.Any<Sorting>(),
        Arg.Any<IServiceProvider>(),
        Arg.Any<CancellationToken>());
}
