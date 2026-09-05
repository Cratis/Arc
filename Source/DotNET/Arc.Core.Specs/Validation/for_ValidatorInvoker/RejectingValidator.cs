// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation;

namespace Cratis.Arc.Validation.for_ValidatorInvoker;

/// <summary>
/// Rejects both properties with authored messages, so a spec can see what a throw displaces.
/// </summary>
public class RejectingValidator : AbstractValidator<Subject>
{
    /// <summary>
    /// The authored message for the first property.
    /// </summary>
    public const string NameMessage = "Name is not acceptable";

    /// <summary>
    /// The authored message for the second property.
    /// </summary>
    public const string EmailMessage = "Email is not acceptable";

    /// <summary>
    /// The state the rule author attaches, which the framework must never claim for itself.
    /// </summary>
    public const string AuthorState = "the author's own state";

    /// <summary>
    /// Initializes a new instance of the <see cref="RejectingValidator"/> class.
    /// </summary>
    public RejectingValidator()
    {
        RuleFor(_ => _.Name).Must(_ => false).WithMessage(NameMessage).WithState(_ => AuthorState);
        RuleFor(_ => _.Email).Must(_ => false).WithMessage(EmailMessage);
    }
}
