// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Identity.Delegation.for_DelegationBoundary;

public class when_the_caller_is_not_delegated : given.a_boundary
{
    readonly List<bool> _results = [];

    void Establish() => _services.AddScoped(_ => _authority);

    async Task Because()
    {
        var boundary = CreateBoundary();
        foreach (var kind in new[] { CallerPrincipalKind.Person, CallerPrincipalKind.OrganizationAutomation, CallerPrincipalKind.ClusterWorkload, CallerPrincipalKind.Runner, (CallerPrincipalKind)99 })
        {
            _results.Add(await boundary.IsAllowed(_request with { Caller = _caller with { PrincipalKind = kind } }));
        }
    }

    [Fact] void should_refuse_even_with_actor_and_grant_metadata() => _results.ShouldEachConformTo(result => !result);
    [Fact] void should_not_call_the_authority() => _authority.ReceivedCalls().ShouldBeEmpty();
}
