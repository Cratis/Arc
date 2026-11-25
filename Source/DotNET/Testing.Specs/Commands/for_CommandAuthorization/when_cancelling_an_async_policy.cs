// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Testing.Commands;

namespace Cratis.Arc.Testing.Commands.for_CommandAuthorization;

public class when_cancelling_an_async_policy : given.an_evaluation
{
    readonly CancellationTokenSource _cancellation = new();
    Exception _caught;
    void Establish() => Behavior.Evaluate = async (_, token) =>
    {
        await Task.Yield();
        await _cancellation.CancelAsync();
        token.ThrowIfCancellationRequested();
        return true;
    };

    async Task Because()
    {
        try { await CommandAuthorization.For(Provider, Principal, new given.ResourceCommand(true), _cancellation.Token); }
        catch (Exception exception) { _caught = exception; }
    }

    [Fact] void should_propagate_cancellation() => (_caught is OperationCanceledException).ShouldBeTrue();
    [Fact] void should_restore_the_principal() => Accessor.Current.ShouldBeNull();
    void Destroy() => _cancellation.Dispose();
}
