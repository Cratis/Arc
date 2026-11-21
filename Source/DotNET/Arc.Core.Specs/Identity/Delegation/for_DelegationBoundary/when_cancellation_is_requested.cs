// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Identity.Delegation.for_DelegationBoundary;

public class when_cancellation_is_requested : given.a_boundary
{
    Exception? _exception;
    bool _authorityResolved;

    void Establish() => _services.AddScoped(_ =>
    {
        _authorityResolved = true;
        return _authority;
    });

    async Task Because()
    {
        try
        {
            await CreateBoundary().IsAllowed(_request, new CancellationToken(true));
        }
        catch (Exception exception)
        {
            _exception = exception;
        }
    }

    [Fact] void should_propagate_cancellation() => _exception.ShouldBeOfExactType<OperationCanceledException>();
    [Fact] void should_not_resolve_the_authority() => _authorityResolved.ShouldBeFalse();
}
