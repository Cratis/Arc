// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryEndpointMapper.when_streaming_with_a_selected_principal;

public class and_no_owned_scope_exists : a_streaming_query_with_a_selected_principal
{
    void Establish() => _useOwnedScope = false;

    async Task Because() => await Run();

    [Fact] void should_stream_through_the_request_own_provider() => _providerSeenByHandler.ShouldBeSame(_context.RequestServices);
    [Fact] void should_not_stream_through_the_owned_provider() => _providerSeenByHandler.ShouldNotBeSame(_ownedScope.ServiceProvider);
    [Fact] void should_not_dispose_the_request_scope() => OwnedScopeWasDisposed().ShouldBeFalse();
}
