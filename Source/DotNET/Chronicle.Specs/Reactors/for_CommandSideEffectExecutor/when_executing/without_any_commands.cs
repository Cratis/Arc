// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reactors.SideEffects;
using Cratis.Monads;

namespace Cratis.Arc.Chronicle.Reactors.for_CommandSideEffectExecutor.when_executing;

public class without_any_commands : given.a_command_side_effect_executor
{
    Result<ReactorSideEffectFailure> _result;

    async Task Because() => _result = await _executor.Execute([]);

    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_execute_any_command() => _commandPipeline.ReceivedCalls().ShouldBeEmpty();
}
