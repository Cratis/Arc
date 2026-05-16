// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using FluentValidation;

namespace Cratis.Arc.Commands.Filters.for_FluentValidationFilter.when_validating;

public class with_validation_context_setup : given.a_fluent_validation_filter
{
    CommandResult _result;
    IValidator _validator;
    FluentValidation.Results.ValidationResult _validationResult;
    TestCommand _command;
    object _capturedInstance;

    void Establish()
    {
        _command = new TestCommand("TestName");
        _context = new CommandContext(_correlationId, typeof(TestCommand), _command, [], new());

        _validator = Substitute.For<IValidator, IObjectValidator>();
        _validationResult = new FluentValidation.Results.ValidationResult();

        _discoverableValidators.TryGet(typeof(TestCommand), out Arg.Any<IValidator>())
            .Returns(x =>
            {
                x[1] = _validator;
                return true;
            });

        ((IObjectValidator)_validator).ValidateObjectAsync(Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                _capturedInstance = call.Arg<object>();
                return _validationResult;
            });
    }

    async Task Because() => _result = await _filter.OnExecution(_context);

    [Fact] void should_return_successful_result() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_call_validator_with_instance() => ((IObjectValidator)_validator).Received(1).ValidateObjectAsync(Arg.Any<object>(), Arg.Any<CancellationToken>());
    [Fact] void should_call_validator_with_correct_instance() => _capturedInstance.ShouldEqual(_command);

    record TestCommand(string Name);
}