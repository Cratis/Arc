// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation.for_DiscoverableValidators;

namespace Cratis.Arc.Commands.Filters.for_FluentValidationFilter.when_executing_command;

/// <summary>
/// Documents the old, broken behavior: resolving the validator from the root provider (as happened before the fix,
/// via <c>Internals.ServiceProvider</c>) cannot resolve a scoped dependency under scope validation, so a read model
/// could never be injected into a validator. The fix is to resolve from the command scope instead.
/// </summary>
public class with_a_scoped_validator_dependency_resolved_from_the_root_provider : given.a_filter_resolving_validators_from_a_real_container
{
    Exception _exception;

    async Task Because()
    {
        var context = new CommandContext(
            _correlationId,
            typeof(CommandWithDependency),
            new CommandWithDependency(Guid.NewGuid()),
            [],
            new(),
            ServiceProvider: _root);

        _exception = await Catch.Exception(async () => await _filter.OnExecution(context));
    }

    [Fact] void should_fail_because_a_scoped_dependency_cannot_be_resolved_from_the_root_provider() => _exception.ShouldNotBeNull();
}
