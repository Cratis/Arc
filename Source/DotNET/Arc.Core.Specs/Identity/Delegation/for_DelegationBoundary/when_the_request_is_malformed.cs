// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Identity.Delegation.for_DelegationBoundary;

public class when_the_request_is_malformed : given.a_boundary
{
    DelegationRequest[] _requests;
    readonly List<bool> _results = [];

    void Establish()
    {
        _services.AddScoped(_ => _authority);
        _requests = [
            null!,
            _request with { Caller = null! },
            _request with { Capability = null! },
            _request with { Capability = " " },
            _request with { Resource = null! },
            _request with { Resource = " " },
            _request with { Environment = null! },
            _request with { Environment = " " }
        ];
    }

    async Task Because()
    {
        var boundary = CreateBoundary();
        foreach (var request in _requests)
        {
            _results.Add(await boundary.IsAllowed(request));
        }
    }

    [Fact] void should_refuse_every_malformed_request() => _results.ShouldEachConformTo(result => !result);
    [Fact] void should_not_call_the_authority() => _authority.ReceivedCalls().ShouldBeEmpty();
}
