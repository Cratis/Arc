// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Specs.CommandResponseHandlerDependency;
using Cratis.Arc.Validation;
using FluentValidation;

namespace Cratis.Arc.ProxyGenerator.Specs.ReferencedConceptValidator;

/// <summary>
/// Validates a concept defined in another referenced assembly.
/// </summary>
public class ExternalNameValidator : ConceptValidator<ExternalName>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExternalNameValidator"/> class.
    /// </summary>
    public ExternalNameValidator() => RuleFor(name => name.Value).MaximumLength(37).WithMessage("External name is too long");
}
