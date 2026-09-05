// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// A concept validator is written against the concept's own value, and a concept declaration takes its rules against
/// its own implied value, so the two line up exactly. Declaring the rule once on the concept is what keeps every
/// command using it from having to repeat it.
/// </summary>
public class a_validator_for_a_concept : Specification
{
    const string Source = """
        using Cratis.Arc.Validation;
        using Cratis.Chronicle.Events;
        using Cratis.Concepts;
        using FluentValidation;

        namespace Library.Authors.Registration;

        public record AuthorName(string Value) : ConceptAs<string>(Value);

        [EventType]
        public record AuthorRegistered(AuthorName Name);

        public class AuthorNameValidator : ConceptValidator<AuthorName>
        {
            public AuthorNameValidator()
            {
                RuleFor(_ => _.Value).NotEmpty().WithMessage("An author name is required");
                RuleFor(_ => _.Value).MaximumLength(200);
            }
        }
        """;

    ApplicationModelAnalysis _analysis;
    ConceptModel _concept;

    void Establish()
    {
        _analysis = Analyzed.Source(Source);
        _concept = _analysis.Model.Concepts.First(_ => _.Name == "AuthorName");
    }

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Library/Feature/Slice/Slice.cs", Source)).ShouldBeEmpty();
    [Fact] void should_attach_the_rules_to_the_concept() => _concept.Validations.Select(_ => _.Kind).ShouldContainOnly([ValidationRuleKind.NotEmpty, ValidationRuleKind.Max]);
    [Fact] void should_carry_the_message() => _concept.Validations.First(_ => _.Kind == ValidationRuleKind.NotEmpty).Message.ShouldEqual("An author name is required");
    [Fact] void should_carry_the_operand() => _concept.Validations.First(_ => _.Kind == ValidationRuleKind.Max).Value.ShouldEqual(200);
    [Fact] void should_report_nothing() => _analysis.Diagnostics.ShouldBeEmpty();
}
