// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Identity.Delegation.for_DelegationBoundary;

public class when_the_delegated_shape_is_incomplete : given.a_boundary
{
    VerifiedCaller[] _callers;
    readonly List<bool> _results = [];

    void Establish()
    {
        _services.AddScoped(_ => _authority);
        _callers = [
            _caller with { AccountKey = null },
            _caller with { Person = null },
            _caller with { GrantId = null },
            _caller with { GrantId = new CallerGrantId(" ") },
            _caller with { Actor = null },
            _caller with { Actor = new CallerActor(" ", "agent") },
            _caller with { Actor = new CallerActor("https://agents.example.test", " ") }
        ];
    }

    async Task Because()
    {
        var boundary = CreateBoundary();
        foreach (var caller in _callers)
        {
            _results.Add(await boundary.IsAllowed(_request with { Caller = caller }));
        }
    }

    [Fact] void should_refuse_every_incomplete_shape() => _results.ShouldEachConformTo(result => !result);
    [Fact] void should_not_call_the_authority() => _authority.ReceivedCalls().ShouldBeEmpty();
}
