// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Cratis.Arc.Commands;
using Cratis.Arc.Validation;
using Cratis.Concepts;
using FluentValidation;

namespace Cratis.Arc.ProxyGenerator.for_ValidationRulesExtractor;

public record OptionalName(string Value) : ConceptAs<string>(Value);

public class OptionalNameValidator : ConceptValidator<OptionalName>
{
    public OptionalNameValidator() => RuleFor(x => x.Value).NotEmpty();
}

public record TestCommandWithNullableConcepts(
    OptionalName? Optional,
    OptionalName Required,
    OptionalName? Explicit,
    [property: Required] OptionalName? Annotated);

public class TestCommandWithNullableConceptsValidator : CommandValidator<TestCommandWithNullableConcepts>
{
    public TestCommandWithNullableConceptsValidator() => RuleFor(x => x.Explicit!).NotNull();
}

public record ConditionalName(string Value) : ConceptAs<string>(Value);

public class ConditionalNameValidator : ConceptValidator<ConditionalName>
{
    public ConditionalNameValidator()
    {
        RuleFor(x => x.Value).MaximumLength(100);
        RuleFor(x => x.Value).NotEmpty().When(x => x.Value.Length > 0);
        RuleFor(x => x.Value).NotEmpty().Unless(x => x.Value.Length == 0);
        RuleFor(x => x.Value).NotEmpty().WhenAsync((x, _) => Task.FromResult(x.Value.Length > 0));
        When(x => x.Value.Length > 0, () => RuleFor(x => x.Value).NotEmpty());
        WhenAsync((x, _) => Task.FromResult(x.Value.Length > 0), () => RuleFor(x => x.Value).NotEmpty());
        RuleFor(x => x.Value).MinimumLength(1).NotEmpty().When(x => x.Value.Length > 0, ApplyConditionTo.CurrentValidator);
    }
}
