// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Testing.Commands;

namespace Cratis.Arc.Testing.Commands.for_CommandAuthorization;

public class when_a_policy_fails : given.an_evaluation
{
    readonly InvalidOperationException _failure = new("fixture failure");
    Exception _caught;
    void Establish() => Behavior.Evaluate = (_, _) => throw _failure;
    async Task Because()
    {
        try { await CommandAuthorization.For(Provider, Principal, new given.ResourceCommand(true)); }
        catch (Exception exception) { _caught = exception; }
    }

    [Fact] void should_propagate_the_original_exception() => _caught.ShouldEqual(_failure);
    [Fact] void should_restore_the_principal() => Accessor.Current.ShouldBeNull();
}
