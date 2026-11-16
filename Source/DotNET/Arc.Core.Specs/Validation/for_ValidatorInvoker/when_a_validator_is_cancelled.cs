// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation;

namespace Cratis.Arc.Validation.for_ValidatorInvoker;

public class when_a_validator_is_cancelled : given.a_validator_invoker
{
    IValidator _validator;
    CancellationTokenSource _cancellation;
    OperationCanceledException _expected;
    Exception? _error;

    void Establish()
    {
        _cancellation = new CancellationTokenSource();
        _expected = new OperationCanceledException(_cancellation.Token);
        _validator = Substitute.For<IValidator>();
        _validator.ValidateAsync(Arg.Any<IValidationContext>(), _cancellation.Token)
            .Returns(Task.FromException<FluentValidation.Results.ValidationResult>(_expected));
    }

    async Task Because() => _error = await Catch.Exception(async () =>
        await _invoker.Invoke(new Subject("name", "email"), _validator, string.Empty, _cancellation.Token));

    void Destroy() => _cancellation.Dispose();

    [Fact] void should_propagate_the_original_cancellation() => ReferenceEquals(_error, _expected).ShouldBeTrue();
}
