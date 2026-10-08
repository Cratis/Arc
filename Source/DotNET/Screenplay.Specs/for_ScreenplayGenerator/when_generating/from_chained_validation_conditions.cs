// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_chained_validation_conditions : a_generated_document
{
    [Theory]
    [InlineData("When", false, false)]
    [InlineData("Unless", false, false)]
    [InlineData("When", true, false)]
    [InlineData("Unless", true, false)]
    [InlineData("When", false, true)]
    [InlineData("Unless", false, true)]
    [InlineData("When", true, true)]
    [InlineData("Unless", true, true)]
    [InlineData("WhenAsync", false, false)]
    [InlineData("UnlessAsync", false, false)]
    public void should_omit_only_the_declarative_validators_held_to_the_condition(string condition, bool currentOnly, bool concept)
    {
        var validatedType = concept ? "AuthorName" : "RegisterAuthor";
        var baseType = concept ? "ConceptValidator" : "CommandValidator";
        var predicate = condition.EndsWith("Async", StringComparison.Ordinal)
            ? "(c, cancellation) => System.Threading.Tasks.Task.FromResult(c.Value.Length > 0)"
            : "c => c.Value.Length > 0";
        var argument = currentOnly ? ", applyConditionTo: ApplyConditionTo.CurrentValidator" : string.Empty;
        var source = $$"""
            using Cratis.Arc.Commands;
            using Cratis.Arc.Commands.ModelBound;
            using Cratis.Arc.Validation;
            using Cratis.Chronicle.Events;
            using Cratis.Concepts;
            using FluentValidation;
            namespace Library.Authors.Registration;
            public record AuthorName(string Value) : ConceptAs<string>(Value);
            [EventType] public record AuthorRegistered(AuthorName Name);
            [Command] public record RegisterAuthor(string Value, AuthorName Name)
            {
                public AuthorRegistered Handle() => new(Name);
            }
            public class {{validatedType}}Validator : {{baseType}}<{{validatedType}}>
            {
                public {{validatedType}}Validator()
                {
                    RuleFor(c => c.Value).NotEmpty().WithMessage("Required")
                        .Length(2, 50).{{condition}}({{predicate}}{{argument}}).WithMessage("Conditional")
                        .MaximumLength(200).WithMessage("Always");
                }
            }
            """;
        Analyzed.ErrorsIn((Analyzed.SlicePath, source)).ShouldBeEmpty();
        Generate((Analyzed.SlicePath, source));
        Result.Source.Contains("not empty message \"Required\"", StringComparison.Ordinal).ShouldEqual(currentOnly);
        Result.Source.ShouldNotContain("min 2");
        Result.Source.ShouldNotContain("max 50");
        Result.Source.ShouldNotContain("message \"Conditional\"");
        Result.Source.ShouldContain("max 200 message \"Always\"");
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableValidationRule &&
            diagnostic.Message.Contains("Length", StringComparison.Ordinal) && diagnostic.Message.Contains(condition, StringComparison.Ordinal)).ShouldBeTrue();
        AssertDocument();
    }
}
