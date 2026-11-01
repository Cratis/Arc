// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_subscribe;

public class and_host_fallback_denies_the_subscription : given.a_scoped_subscription_container
{
    ScopedProbe _probe;

    void Establish() =>
        _fallbackAuthorization.IsAuthorized(Arg.Any<IHttpRequestContext>(), Arg.Any<ClaimsPrincipal>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                // Admission runs before the query; resolve through the subscription context to observe its scope.
                _probe = _httpRequestContextAccessor.Current.RequestServices.GetRequiredService<ScopedProbe>();
                return false;
            });

    async Task Because() => await RunConnection(() => Task.CompletedTask);

    [Fact] void should_resolve_through_a_scope_of_its_own() => _probe.ShouldNotBeSame(_rootProbe);
    [Fact] void should_reject_the_subscribe() => _subscribeStatusCodes[FirstQueryId].ShouldEqual(401);
    [Fact] void should_dispose_the_probe_exactly_once() => _probe.Disposals.ShouldEqual(1);
}
