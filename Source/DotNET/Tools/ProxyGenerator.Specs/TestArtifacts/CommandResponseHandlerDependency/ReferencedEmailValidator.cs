// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using FluentValidation;

namespace Cratis.Arc.ProxyGenerator.Specs.CommandResponseHandlerDependency;

/// <summary>
/// Validates an email concept declared in a referenced assembly.
/// </summary>
public class ReferencedEmailValidator : ConceptValidator<ReferencedEmail>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReferencedEmailValidator"/> class.
    /// </summary>
    public ReferencedEmailValidator() => RuleFor(email => email.Value).EmailAddress().WithMessage("Referenced email is invalid");
}
