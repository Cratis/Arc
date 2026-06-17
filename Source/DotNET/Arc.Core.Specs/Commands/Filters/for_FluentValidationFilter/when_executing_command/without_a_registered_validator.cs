// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Arc.Validation.for_DiscoverableValidators;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Commands.Filters.for_FluentValidationFilter.when_executing_command;

/// <summary>
/// The end-to-end guarantee for read-model validators: a validator that takes a scoped dependency — the stand-in for
/// a read model resolved for the command's event source id — runs through the filter even though it is NOT registered
/// in the container. It is constructed on demand from the command scope, with its dependency resolved from that same
/// scope, exactly as the command handler and Provide method resolve theirs.
/// </summary>
public class without_a_registered_validator : Specification
{
    FluentValidationFilter _filter;
    ServiceProvider _root = null!;
    CorrelationId _correlationId;
    CommandResult _result;
    Exception _exception;

    void Establish()
    {
        _correlationId = CorrelationId.New();
        _filter = new FluentValidationFilter(new DiscoverableValidators(Cratis.Types.Types.Instance));

        // The scoped dependency is registered; the validator itself is intentionally NOT registered.
        _root = new ServiceCollection()
            .AddScoped(_ => new CommandDependency { IsAllowed = false })
            .BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

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

    void Destroy() => _root.Dispose();

    [Fact] void should_not_throw() => _exception.ShouldBeNull();
    [Fact] void should_run_the_unregistered_validator_against_the_scoped_dependency() => _result.ValidationResults.First().Message.ShouldEqual("The dependency does not allow this command.");
}
