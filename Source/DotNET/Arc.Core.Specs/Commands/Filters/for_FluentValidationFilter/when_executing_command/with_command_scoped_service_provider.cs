// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using FluentValidationResult = FluentValidation.Results.ValidationResult;

namespace Cratis.Arc.Commands.Filters.for_FluentValidationFilter.when_executing_command;

public class with_command_scoped_service_provider : given.a_fluent_validation_filter
{
    IServiceProvider _commandScopedProvider;
    IValidator _validator;
    TestCommand _command;

    void Establish()
    {
        _command = new TestCommand("Name");
        _commandScopedProvider = new ServiceCollection().BuildServiceProvider();
        _context = new CommandContext(_correlationId, typeof(TestCommand), _command, [], new(), ServiceProvider: _commandScopedProvider);

        _validator = Substitute.For<IValidator>();
        _validator.ValidateAsync(Arg.Any<IValidationContext>(), Arg.Any<CancellationToken>()).Returns(new FluentValidationResult());

        _discoverableValidators.TryGet(typeof(TestCommand), _commandScopedProvider, out Arg.Any<IValidator>())
            .Returns(x =>
            {
                x[2] = _validator;
                return true;
            });
    }

    async Task Because() => await _filter.OnExecution(_context);

    [Fact] void should_resolve_the_validator_from_the_command_scoped_provider() =>
        _discoverableValidators.Received(1).TryGet(typeof(TestCommand), _commandScopedProvider, out Arg.Any<IValidator>());
    [Fact] void should_not_fall_back_to_the_ambient_service_provider() =>
        _discoverableValidators.DidNotReceive().TryGet(typeof(TestCommand), out Arg.Any<IValidator>());

    record TestCommand(string Name);
}
