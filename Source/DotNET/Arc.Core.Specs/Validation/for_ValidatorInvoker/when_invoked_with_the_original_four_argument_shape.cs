// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation;

namespace Cratis.Arc.Validation.for_ValidatorInvoker;

/// <summary>
/// Compile-level guard: calls written against the original method, including a <see langword="default"/> cancellation token,
/// must keep binding without ambiguity now that an overload carrying a <see cref="ValidationTargetKind"/> exists.
/// </summary>
public class when_invoked_with_the_original_four_argument_shape : given.a_validator_invoker
{
    class SubjectValidator : AbstractValidator<Subject>
    {
        public SubjectValidator() => RuleFor(subject => subject.Name).NotEmpty().WithMessage("Name is required");
    }

    IEnumerable<ValidationResult> _concreteWithDefault;
    IEnumerable<ValidationResult> _interfaceWithDefault;
    IEnumerable<ValidationResult> _interfaceWithoutToken;

    async Task Because()
    {
        var subject = new Subject(string.Empty, "valid");
        IValidatorInvoker invoker = _invoker;
        _concreteWithDefault = await _invoker.Invoke(subject, new SubjectValidator(), string.Empty, default);
        _interfaceWithDefault = await invoker.Invoke(subject, new SubjectValidator(), string.Empty, default);
        _interfaceWithoutToken = await invoker.Invoke(subject, new SubjectValidator(), string.Empty);
    }

    [Fact] void should_validate_through_the_concrete_invoker() => _concreteWithDefault.Single().Message.ShouldEqual("Name is required");
    [Fact] void should_validate_through_the_interface_with_a_default_token() => _interfaceWithDefault.Single().Message.ShouldEqual("Name is required");
    [Fact] void should_validate_through_the_interface_without_a_token() => _interfaceWithoutToken.Single().Message.ShouldEqual("Name is required");
}
