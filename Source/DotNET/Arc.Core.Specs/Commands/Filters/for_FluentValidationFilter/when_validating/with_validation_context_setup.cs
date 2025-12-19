// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation;
using FluentValidation.Results;

namespace Cratis.Arc.Commands.Filters.for_FluentValidationFilter.when_validating;

public class with_validation_context_setup : given.a_fluent_validation_filter
{
    CommandResult _result;
    IValidator _validator;
    ValidationResult _validationResult;
    TestCommand _command;
    IValidationContext _capturedContext;

    void Establish()
    {
        _command = new TestCommand("TestName");
        _context = new CommandContext(_correlationId, typeof(TestCommand), _command, [], new());

        _validator = Substitute.For<IValidator>();
        _validationResult = new ValidationResult();

        _discoverableValidators.TryGet(typeof(TestCommand), out Arg.Any<IValidator>())
            .Returns(x =>
            {
                x[1] = _validator;
                return true;
            });

        _validator.ValidateAsync(Arg.Any<IValidationContext>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                _capturedContext = call.Arg<IValidationContext>();
                return _validationResult;
            });
    }

    async Task Because() => _result = await _filter.OnExecution(_context);

    [Fact] void should_return_successful_result() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_call_validator_with_validation_context() => _validator.Received(1).ValidateAsync(Arg.Any<IValidationContext>(), Arg.Any<CancellationToken>());
    [Fact] void should_create_validation_context_with_correct_instance() => _capturedContext.InstanceToValidate.ShouldEqual(_command);

    record TestCommand(string Name);
}