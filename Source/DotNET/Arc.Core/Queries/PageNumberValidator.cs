// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using FluentValidation;

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents a validator for <see cref="PageNumber"/>.
/// </summary>
public class PageNumberValidator : ConceptValidator<PageNumber>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PageNumberValidator"/> class.
    /// </summary>
    public PageNumberValidator()
    {
        RuleFor(x => x.Value).GreaterThanOrEqualTo(0).WithMessage("Page number must be greater than or equal to 0");
    }
}
