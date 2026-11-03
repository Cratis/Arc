// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryEndpointMapper.when_streaming_with_a_selected_principal;

public class and_the_streaming_handler_throws : a_streaming_query_with_a_selected_principal
{
    void Establish() => _handler = () => throw new InvalidOperationException("stream failed");

    async Task Because() => await Run();

    [Fact] void should_surface_the_failure() => Failure.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_still_dispose_the_owned_scope_exactly_once() => OwnedScopeWasDisposed().ShouldBeTrue();
}
