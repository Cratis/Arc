// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Specs.CommandResponseHandlerDependency;
using Cratis.Arc.Validation;
using FluentValidation;

namespace Cratis.Arc.ProxyGenerator.Specs.ReferencedConceptValidator;

/// <summary>
/// Supplies validation rules to validators in downstream assemblies.
/// </summary>
public abstract class TransitiveNameValidatorBase : BaseValidator<TransitiveName>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TransitiveNameValidatorBase"/> class.
    /// </summary>
    protected TransitiveNameValidatorBase() => RuleFor(name => name.Value).MaximumLength(23).WithMessage("Transitive name is too long");
}
