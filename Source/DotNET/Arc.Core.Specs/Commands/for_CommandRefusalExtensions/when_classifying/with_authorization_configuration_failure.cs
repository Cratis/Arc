// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Arc.Commands.for_CommandRefusalExtensions.when_classifying;

public class with_authorization_configuration_failure : for_CommandPipeline.given.a_command_pipeline_and_a_handler_for_command
{
    CommandResult _result;
    CommandResult _merged;
    CommandResult<object> _typed;

    async Task Because()
    {
        var services = Substitute.For<IServiceProvider>();
        _result = await _commandPipeline.Execute(_command, services);
        _merged = CommandResult.Success(_correlationId);
        _merged.MergeWith(_result);
        _typed = await _commandPipeline.Execute<object>(_command, services);
    }

    [Fact] void should_keep_the_unauthorized_outcome() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_not_require_an_exception_to_exclude_a_refusal() => _result.HasExceptions.ShouldBeFalse();
    [Fact] void should_not_classify_a_contract_failure_as_authorization() => _result.GetRefusal().ShouldBeNull();
    [Fact] void should_preserve_the_non_refusal_through_merge() => _merged.GetRefusal().ShouldBeNull();
    [Fact] void should_preserve_the_non_refusal_through_typed_execution() => _typed.GetRefusal().ShouldBeNull();
    [Fact] void should_preserve_the_outward_json() => JsonSerializer.Serialize(_result).ShouldEqual(JsonSerializer.Serialize(CommandResult.Unauthorized(_correlationId)));
}
