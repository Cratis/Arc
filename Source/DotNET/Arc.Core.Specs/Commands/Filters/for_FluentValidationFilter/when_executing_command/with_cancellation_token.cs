// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation;
using FluentValidationResult = FluentValidation.Results.ValidationResult;

namespace Cratis.Arc.Commands.Filters.for_FluentValidationFilter.when_executing_command;

public class with_cancellation_token : given.a_fluent_validation_filter
{
    IValidator _validator;
    readonly CancellationTokenSource _cancellationTokenSource = new();

    void Establish()
    {
        var command = new TestCommand();
        _context = new CommandContext(_correlationId, typeof(TestCommand), command, [], new(), CancellationToken: _cancellationTokenSource.Token);

        _validator = Substitute.For<IValidator>();
        _validator.ValidateAsync(Arg.Any<IValidationContext>(), Arg.Any<CancellationToken>()).Returns(new FluentValidationResult());

        _discoverableValidators.TryGet(typeof(TestCommand), out Arg.Any<IValidator>())
            .Returns(x =>
            {
                x[1] = _validator;
                return true;
            });
    }

    async Task Because() => await _filter.OnExecution(_context);

    void Destroy() => _cancellationTokenSource.Dispose();

    [Fact] void should_call_validator_with_the_command_context_cancellation_token() =>
        _validator.Received(1).ValidateAsync(Arg.Any<IValidationContext>(), _cancellationTokenSource.Token);

    public class TestCommand;
}
