// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Identity.Delegation.for_DelegationBoundary;

public class when_cancellation_occurs_during_the_check : given.a_boundary
{
    readonly CancellationTokenSource _cancellation = new();
    Exception? _exception;
    CancellationToken _observedToken;

    void Establish()
    {
        _services.AddScoped(_ => _authority);
        _authority.IsAllowed(_request, Arg.Any<CancellationToken>()).Returns(call =>
        {
            _observedToken = call.Arg<CancellationToken>();
            _cancellation.Cancel();
            return true;
        });
    }

    async Task Because()
    {
        try
        {
            await CreateBoundary().IsAllowed(_request, _cancellation.Token);
        }
        catch (Exception exception)
        {
            _exception = exception;
        }
    }

    void Destroy() => _cancellation.Dispose();

    [Fact] void should_propagate_cancellation_even_after_a_positive_decision() => _exception.ShouldBeOfExactType<OperationCanceledException>();
    [Fact] void should_forward_the_token() => _observedToken.ShouldEqual(_cancellation.Token);
}
