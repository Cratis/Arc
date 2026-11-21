// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Identity.Delegation.for_DelegationBoundary;

public class when_multiple_authorities_are_registered : given.a_boundary
{
    void Establish()
    {
        _services.AddScoped(_ => _authority);
        _services.AddScoped(_ => _authority);
    }

    async Task Because() => _result = await CreateBoundary().IsAllowed(_request);

    [Fact] void should_refuse() => _result.ShouldBeFalse();
    [Fact] void should_not_choose_an_authority() => _authority.ReceivedCalls().ShouldBeEmpty();
}
