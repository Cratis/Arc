// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using FluentValidation;

namespace Cratis.Arc.ProxyGenerator.for_ValidationRulesExtractor;

/// <summary>
/// A model carrying one DataAnnotations attribute per property.
/// </summary>
public class AnnotatedModel
{
    [Required(ErrorMessage = "required message")]
    public string Required { get; set; } = string.Empty;

    [StringLength(10, MinimumLength = 2)]
    public string BothLengths { get; set; } = string.Empty;

    [StringLength(0, MinimumLength = 3)]
    public string MinimumOnly { get; set; } = string.Empty;

    [StringLength(10)]
    public string MaximumOnly { get; set; } = string.Empty;

    [MinLength(3)]
    public string MinLengthAttribute { get; set; } = string.Empty;

    [MaxLength(5)]
    public string MaxLengthAttribute { get; set; } = string.Empty;

    [Range(1, 10)]
    public int Ranged { get; set; }

    [RegularExpression("^a+$")]
    public string Pattern { get; set; } = string.Empty;

    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Phone]
    public string Phone { get; set; } = string.Empty;

    [Url]
    public string Url { get; set; } = string.Empty;

    [CreditCard]
    public string Card { get; set; } = string.Empty;

    [Display(Name = "ignored")]
    public string Ignored { get; set; } = string.Empty;
}

/// <summary>
/// A model validated by <see cref="ValidatedModelValidator"/>.
/// </summary>
public class ValidatedModel
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Bounded { get; set; } = string.Empty;
    public string AtLeast { get; set; } = string.Empty;
    public string AtMost { get; set; } = string.Empty;
    public string Exactly { get; set; } = string.Empty;
    public int Number { get; set; }
    public int Small { get; set; }
    public int Big { get; set; }
    public int Least { get; set; }
    public string Pattern { get; set; } = string.Empty;
    public string Conditional { get; set; } = string.Empty;
    public string Warned { get; set; } = string.Empty;
    public string Informed { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public string Unvalidated { get; set; } = string.Empty;
}

/// <summary>
/// Validator exercising every rule kind the extractor understands.
/// </summary>
public class ValidatedModelValidator : AbstractValidator<ValidatedModel>
{
    public ValidatedModelValidator()
    {
        RuleFor(_ => _.Name).NotEmpty().WithMessage("name required");
        RuleFor(_ => _.Email).EmailAddress();
        RuleFor(_ => _.Bounded).Length(2, 5);
        RuleFor(_ => _.AtLeast).MinimumLength(3);
        RuleFor(_ => _.AtMost).MaximumLength(8);
        RuleFor(_ => _.Exactly).Length(4);
        RuleFor(_ => _.Number).GreaterThan(5);
        RuleFor(_ => _.Small).LessThan(5);
        RuleFor(_ => _.Big).GreaterThanOrEqualTo(7);
        RuleFor(_ => _.Least).LessThanOrEqualTo(9);
        RuleFor(_ => _.Pattern).Matches("^b+$");
        RuleFor(_ => _.Conditional).NotEmpty().When(_ => _.Name.Length > 0);
        RuleFor(_ => _.Warned).NotNull().WithSeverity(Severity.Warning);
        RuleFor(_ => _.Informed).NotNull().WithSeverity(Severity.Info);
        RuleFor(_ => _.Date).GreaterThan(DateOnly.MinValue);
    }
}
