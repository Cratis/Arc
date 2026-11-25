// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;

namespace Cratis.Arc.Testing.Commands.for_CommandAuthorization;

public class when_evaluating_inside_an_active_command : given.an_evaluation
{
    async Task Because()
    {
        await using var scenario = new CommandScenario<given.NestedEvaluationCommand>();
        var result = await scenario.Execute(new given.NestedEvaluationCommand(Provider, Principal));
        Result = ((CommandResult<CommandAuthorizationResult>)result).Response;
    }

    [Fact] void should_authorize_with_the_explicit_principal() => Result.ShouldBeAuthorized();
    [Fact] void should_restore_the_standalone_principal() => Accessor.Current.ShouldBeNull();
}
