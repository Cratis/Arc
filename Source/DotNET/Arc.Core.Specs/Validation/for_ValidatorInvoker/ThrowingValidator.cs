// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation;

namespace Cratis.Arc.Validation.for_ValidatorInvoker;

/// <summary>
/// Rejects one property with an authored message and throws on the next, which is the shape that matters: the throw
/// does not merely add a result, it replaces the one already authored.
/// </summary>
public class ThrowingValidator : AbstractValidator<Subject>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ThrowingValidator"/> class.
    /// </summary>
    public ThrowingValidator()
    {
        RuleFor(_ => _.Name).Must(_ => false).WithMessage(RejectingValidator.NameMessage);
        RuleFor(_ => _.Email).Must(_ => throw new InvalidOperationException("boom"));
    }
}
