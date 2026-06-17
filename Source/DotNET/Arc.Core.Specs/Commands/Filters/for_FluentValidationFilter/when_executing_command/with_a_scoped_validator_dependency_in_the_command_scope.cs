// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation.for_DiscoverableValidators;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Commands.Filters.for_FluentValidationFilter.when_executing_command;

/// <summary>
/// The regression spec for read-model injection into validators. Without resolving the validator from the command
/// scope, the scoped dependency would throw under scope validation (the old behavior, where validators resolved from
/// the root provider). With the fix it resolves cleanly and the validator runs against it.
/// </summary>
public class with_a_scoped_validator_dependency_in_the_command_scope : given.a_filter_resolving_validators_from_a_real_container
{
    CommandResult _result;
    Exception _exception;

    async Task Because()
    {
        await using var commandScope = _root.CreateAsyncScope();
        var context = new CommandContext(
            _correlationId,
            typeof(CommandWithDependency),
            new CommandWithDependency(Guid.NewGuid()),
            [],
            new(),
            ServiceProvider: commandScope.ServiceProvider);

        _exception = await Catch.Exception(async () => _result = await _filter.OnExecution(context));
    }

    [Fact] void should_resolve_the_scoped_dependency_without_throwing() => _exception.ShouldBeNull();
    [Fact] void should_run_the_validator_against_the_scoped_dependency() => _result.ValidationResults.First().Message.ShouldEqual("The dependency does not allow this command.");
}
