// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Identity.Delegation.for_DelegationBoundary;

public class when_the_authority_cancels_the_requested_check : given.a_boundary
{
    readonly CancellationTokenSource _cancellation = new();
    Exception? _exception;

    void Establish()
    {
        _services.AddScoped(_ => _authority);
        _authority.IsAllowed(_request, Arg.Any<CancellationToken>()).Returns(async _ =>
        {
            await _cancellation.CancelAsync();
            return await Task.FromCanceled<bool>(_cancellation.Token);
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

    [Fact] void should_propagate_cancellation() => _exception.ShouldBeOfExactType<TaskCanceledException>();
}
