// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Concepts;
using FluentValidation;

namespace Cratis.Arc.ProxyGenerator.for_ValidationRulesExtractor;

public record ConstrainedText(string Value) : ConceptAs<string>(Value);

public class ConstrainedTextValidator : ConceptValidator<ConstrainedText>
{
    public ConstrainedTextValidator()
    {
        RuleFor(x => x.Value).NotNull().NotEmpty();
        RuleFor(x => x.Value).MinimumLength(1);
        RuleFor(x => x.Value).MaximumLength(10).WithMessage("At most ten characters").WithSeverity(Severity.Warning);
        RuleFor(x => x.Value).Length(1, 10);
        RuleFor(x => x.Value).Length(5);
        RuleFor(x => x.Value).EmailAddress();
        RuleFor(x => x.Value).Matches(".*");
    }
}

public record ConstrainedNumber(int Value) : ConceptAs<int>(Value);

public class ConstrainedNumberValidator : ConceptValidator<ConstrainedNumber>
{
    public ConstrainedNumberValidator()
    {
        RuleFor(x => x.Value).NotEmpty();
        RuleFor(x => x.Value).GreaterThan(0);
        RuleFor(x => x.Value).GreaterThanOrEqualTo(1);
        RuleFor(x => x.Value).LessThan(11);
        RuleFor(x => x.Value).LessThanOrEqualTo(10);
    }
}
