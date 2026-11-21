// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Identity.Delegation.for_DelegationBoundary;

public class when_resolving_the_authority_fails : given.a_boundary
{
    void Establish() => _services.AddScoped<IDelegationAuthority>(_ => throw new InvalidOperationException("unavailable"));

    async Task Because() => _result = await CreateBoundary().IsAllowed(_request);

    [Fact] void should_refuse() => _result.ShouldBeFalse();
}
