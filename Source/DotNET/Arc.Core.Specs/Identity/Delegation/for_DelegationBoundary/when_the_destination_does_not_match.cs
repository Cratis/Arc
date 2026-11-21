// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Identity.Delegation.for_DelegationBoundary;

public class when_the_destination_does_not_match : given.a_boundary
{
    readonly List<bool> _results = [];

    void Establish() => _services.AddScoped(_ => _authority);

    async Task Because()
    {
        var boundary = CreateBoundary();
        _results.Add(await boundary.IsAllowed(_request with { Resource = _caller.Resource.ToUpperInvariant() }));
        _results.Add(await boundary.IsAllowed(_request with { Environment = _caller.Environment.ToUpperInvariant() }));
    }

    [Fact] void should_require_exact_matches() => _results.ShouldEachConformTo(result => !result);
    [Fact] void should_not_call_the_authority() => _authority.ReceivedCalls().ShouldBeEmpty();
}
