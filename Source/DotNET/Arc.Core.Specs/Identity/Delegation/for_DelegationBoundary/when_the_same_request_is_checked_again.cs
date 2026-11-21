// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Identity.Delegation.for_DelegationBoundary;

public class when_the_same_request_is_checked_again : given.a_boundary
{
    bool _firstResult;

    void Establish()
    {
        _services.AddScoped(_ => _authority);
        _authority.IsAllowed(_request, Arg.Any<CancellationToken>()).Returns(true, false);
    }

    async Task Because()
    {
        var boundary = CreateBoundary();
        _firstResult = await boundary.IsAllowed(_request);
        _result = await boundary.IsAllowed(_request);
    }

    [Fact] void should_allow_the_first_current_check() => _firstResult.ShouldBeTrue();
    [Fact] void should_not_cache_an_earlier_allow() => _result.ShouldBeFalse();
    [Fact] void should_check_the_authority_on_every_call() => _authority.Received(2).IsAllowed(_request, Arg.Any<CancellationToken>());
}
