// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryEndpointMapper.when_streaming_with_a_selected_principal;

public class and_an_owned_scope_exists : a_streaming_query_with_a_selected_principal
{
    async Task Because() => await Run();

    [Fact] void should_stream_through_the_owned_provider() => _providerSeenByHandler.ShouldBeSame(_ownedScope.ServiceProvider);
    [Fact] void should_keep_the_owned_scope_alive_while_streaming() => _ownedScopeDisposedDuringHandler.ShouldBeFalse();
    [Fact] void should_dispose_the_owned_scope_exactly_once_afterwards() => OwnedScopeWasDisposed().ShouldBeTrue();
}
