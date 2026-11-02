// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation;

namespace Cratis.Arc.Validation.for_ValidatorInvoker;

/// <summary>
/// A validator that reads the operation's tenant from its validation context, directly and in a child validator.
/// </summary>
public class ReadingValidator : AbstractValidator<Subject>
{
    /// <summary>
    /// The tenants read, in order.
    /// </summary>
    public static readonly List<string> Tenants = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadingValidator"/> class.
    /// </summary>
    public ReadingValidator()
    {
        RuleFor(_ => _.Name).Custom((_, context) => Tenants.Add(context.GetTenant().Value));
        RuleFor(_ => _).SetValidator(new ChildReadingValidator());
    }

    class ChildReadingValidator : AbstractValidator<Subject>
    {
        public ChildReadingValidator() =>
            RuleFor(_ => _.Email).Custom((_, context) => Tenants.Add(context.GetTenant().Value));
    }
}
