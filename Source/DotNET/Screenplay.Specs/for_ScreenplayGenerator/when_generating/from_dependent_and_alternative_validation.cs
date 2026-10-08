// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_dependent_and_alternative_validation : a_generated_document
{
    [Theory]
    [InlineData("DependentRules", false)]
    [InlineData("DependentRules", true)]
    [InlineData("When", false)]
    [InlineData("When", true)]
    [InlineData("Unless", false)]
    [InlineData("Unless", true)]
    public void should_keep_the_unconditional_predicate_and_omit_only_callback_rules(string block, bool concept)
    {
        var validatedType = concept ? "AuthorName" : "RegisterAuthor";
        var baseType = concept ? "ConceptValidator" : "CommandValidator";
        var rules = block == "DependentRules"
            ? "RuleFor(c => c.Value).Must(IsKnownName).WithMessage(\"Known\").DependentRules(() => { RuleFor(c => c.Other).NotEmpty(); });"
            : $$"""
                RuleFor(c => c.Value).Must(IsKnownName).WithMessage("Known");
                {{block}}(c => c.Value.Length > 0, () => { RuleFor(c => c.Other).MaximumLength(10); })
                    .Otherwise(() => { RuleFor(c => c.Other).NotEmpty(); });
                """;
        var source = $$"""
            using Cratis.Arc.Commands;
            using Cratis.Arc.Commands.ModelBound;
            using Cratis.Arc.Validation;
            using Cratis.Chronicle.Events;
            using Cratis.Concepts;
            using FluentValidation;
            namespace Library.Authors.Registration;
            public record AuthorName(string Value) : ConceptAs<string>(Value)
            {
                public string Other => Value;
            }
            [EventType] public record AuthorRegistered(AuthorName Name);
            [Command] public record RegisterAuthor(string Value, string Other, AuthorName Name)
            {
                public AuthorRegistered Handle() => new(Name);
            }
            public class {{validatedType}}Validator : {{baseType}}<{{validatedType}}>
            {
                public {{validatedType}}Validator()
                {
                    {{rules}}
                    RuleFor(c => c.Value).MaximumLength(200);
                }
                static bool IsKnownName(string name) => name == "Apollo";
            }
            """;
        Analyzed.ErrorsIn((Analyzed.SlicePath, source)).ShouldBeEmpty();
        Generate((Analyzed.SlicePath, source));
        Result.Source.ShouldContain("rule IsKnownName message \"Known\"");
        Result.Source.ShouldContain("max 200");
        Result.Source.ShouldNotContain("not empty");
        Result.Source.ShouldNotContain("max 10");
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableValidationRule &&
            diagnostic.Message.Contains("IsKnownName", StringComparison.Ordinal)).ShouldBeFalse();
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableValidationRule &&
            diagnostic.Message.Contains("NotEmpty", StringComparison.Ordinal) &&
            diagnostic.Message.Contains(block == "DependentRules" ? "DependentRules" : "Otherwise", StringComparison.Ordinal)).ShouldBeTrue();
        AssertDocument();
    }
}
