// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Validation.for_ValidatorInvoker.given;

public class a_validator_invoker : Specification
{
    protected ValidatorInvoker _invoker;
    protected ILogger<ValidatorInvoker> _logger;

    void Establish()
    {
        _logger = Substitute.For<ILogger<ValidatorInvoker>>();
        _invoker = new ValidatorInvoker(_logger);
    }

    public record Subject(string Name, string Email);

    /// <summary>
    /// Rejects both properties with authored messages, so a spec can see what a throw displaces.
    /// </summary>
    public class RejectingValidator : AbstractValidator<Subject>
    {
        public const string NameMessage = "Name is not acceptable";
        public const string EmailMessage = "Email is not acceptable";

        public RejectingValidator()
        {
            RuleFor(_ => _.Name).Must(_ => false).WithMessage(NameMessage).WithState(_ => "author's state");
            RuleFor(_ => _.Email).Must(_ => false).WithMessage(EmailMessage);
        }
    }

    /// <summary>
    /// Rejects one property with an authored message and throws on the next, which is the shape that matters: the
    /// throw does not merely add a result, it replaces the one already authored.
    /// </summary>
    public class ThrowingValidator : AbstractValidator<Subject>
    {
        public ThrowingValidator()
        {
            RuleFor(_ => _.Name).Must(_ => false).WithMessage(RejectingValidator.NameMessage);
            RuleFor(_ => _.Email).Must(_ => throw new InvalidOperationException("boom"));
        }
    }
}
