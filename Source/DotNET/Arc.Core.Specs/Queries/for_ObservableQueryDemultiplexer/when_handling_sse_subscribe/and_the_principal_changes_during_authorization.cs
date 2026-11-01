// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_subscribe;

public class and_the_principal_changes_during_authorization : given.a_scoped_subscription_container
{
    IServiceScope _ownedScope;
    ScopedProbe _ownedProbe;
    IServiceProvider _servicesAfterEstablishment;

    void Establish() => _result = () =>
    {
        _ownedScope = _guardedServiceProvider.CreateScope();
        _ownedProbe = _ownedScope.ServiceProvider.GetRequiredService<ScopedProbe>();
        var result = QueryResult.Success(CorrelationId.New());
        result.Data = _subject;
        result.AuthorizedPrincipal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "bob")], "test"));
        result.OwnedScope = _ownedScope;
        return result;
    };

    async Task Because() => await RunConnection(() =>
    {
        _servicesAfterEstablishment = _performContexts.Single().RequestServices;
        return Task.CompletedTask;
    });

    [Fact] void should_resolve_through_the_owned_scope_after_the_principal_changed() => _servicesAfterEstablishment.ShouldBeSame(_ownedScope.ServiceProvider);
    [Fact] void should_dispose_the_owned_probe_exactly_once() => _ownedProbe.Disposals.ShouldEqual(1);
    [Fact] void should_dispose_the_subscription_probe_exactly_once() => _resolvedDuringPerform.Single().Disposals.ShouldEqual(1);
    [Fact] void should_dispose_the_owned_scope_before_the_subscription_scope() => (_ownedProbe.DisposedAt < _resolvedDuringPerform.Single().DisposedAt).ShouldBeTrue();
}
