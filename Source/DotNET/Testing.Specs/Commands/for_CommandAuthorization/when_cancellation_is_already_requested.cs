// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Testing.Commands;

namespace Cratis.Arc.Testing.Commands.for_CommandAuthorization;

public class when_cancellation_is_already_requested : given.an_evaluation
{
    Exception _caught;
    async Task Because()
    {
        try { await CommandAuthorization.For(Provider, Principal, new given.ResourceCommand(true), new CancellationToken(true)); }
        catch (Exception exception) { _caught = exception; }
    }

    [Fact] void should_propagate_cancellation() => (_caught is OperationCanceledException).ShouldBeTrue();
    [Fact] void should_not_evaluate() => Behavior.PolicyCalls.ShouldEqual(0);
}
