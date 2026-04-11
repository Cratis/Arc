// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using FluentValidation;

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents a validator for <see cref="PageSize"/>.
/// </summary>
public class PageSizeValidator : ConceptValidator<PageSize>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PageSizeValidator"/> class.
    /// </summary>
    public PageSizeValidator()
    {
        RuleFor(x => x.Value).GreaterThan(0).WithMessage("Page size must be greater than 0");
    }
}
