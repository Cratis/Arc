// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using FluentValidation;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_ProxyGeneration;

/// <summary>
/// Carries one property per client-projectable validation rule shape, so the generation scenario can pin down the
/// exact TypeScript each shape produces — and that the shapes which cannot be projected are dropped cleanly.
/// </summary>
public class CommandWithEveryRuleShape
{
    public string Name { get; set; } = string.Empty;
    public string? Nickname { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public string Pin { get; set; } = string.Empty;
    public int Age { get; set; }
    public int Seats { get; set; }
    public decimal Price { get; set; }
    public string ApiRoute { get; set; } = string.Empty;
    public string BirthDate { get; set; } = string.Empty;
    public DateOnly When { get; set; }
}

/// <summary>
/// Declares every rule shape the extractor can project, plus the ones it deliberately drops: a comparison over a
/// non-numeric value has no client rule, and a lazily declared message is resolved when it does not read the
/// instance.
/// </summary>
public class CommandWithEveryRuleShapeValidator : BaseValidator<CommandWithEveryRuleShape>
{
    public CommandWithEveryRuleShapeValidator()
    {
        RuleFor(x => x.Name).NotEmpty().Length(2, 50);
        RuleFor(x => x.Nickname).NotNull();
        RuleFor(x => x.Email).EmailAddress();
        RuleFor(x => x.Bio).MinimumLength(10).MaximumLength(200);
        RuleFor(x => x.Pin).Length(4).WithMessage(_ => "Pin must be exactly four digits");
        RuleFor(x => x.Age).GreaterThanOrEqualTo(18).LessThan(150);
        RuleFor(x => x.Seats).GreaterThan(0).LessThanOrEqualTo(64);
        RuleFor(x => x.Price).GreaterThan(0.5m);
        RuleFor(x => x.ApiRoute).Matches("^/api/[a-z]+$");
        RuleFor(x => x.BirthDate).Matches(@"^\d{2}\/\d{2}$");
        RuleFor(x => x.When).GreaterThan(DateOnly.MinValue);
    }
}
