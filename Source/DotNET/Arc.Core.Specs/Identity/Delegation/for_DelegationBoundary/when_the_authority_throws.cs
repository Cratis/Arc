// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Identity.Delegation.for_DelegationBoundary;

public class when_the_authority_throws : given.a_boundary
{
    void Establish()
    {
        _services.AddScoped(_ => _authority);
        _authority.IsAllowed(_request, Arg.Any<CancellationToken>()).Returns(Task.FromException<bool>(new InvalidOperationException("unavailable")));
    }

    async Task Because() => _result = await CreateBoundary().IsAllowed(_request);

    [Fact] void should_refuse() => _result.ShouldBeFalse();
}
