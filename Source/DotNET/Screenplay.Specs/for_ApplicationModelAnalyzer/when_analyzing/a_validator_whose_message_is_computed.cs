// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// A message a lambda computes at runtime - an interpolation, a call, a culture-dependent lookup - has no
/// compile-time constant to read, so there is nothing to write into the document. It stays reported rather than
/// guessed at, which is what keeps the recovery of the constant forms honest.
/// </summary>
public class a_validator_whose_message_is_computed : Specification
{
    const string Source = """
        using Cratis.Arc.Commands;
        using Cratis.Arc.Commands.ModelBound;
        using FluentValidation;

        namespace Library.Authors.Registration;

        [Command]
        public record RegisterAuthor(string Name)
        {
            public void Handle()
            {
            }
        }

        public class RegisterAuthorValidator : CommandValidator<RegisterAuthor>
        {
            public RegisterAuthorValidator()
            {
                RuleFor(_ => _.Name).NotEmpty().WithMessage(_ => $"The name '{_.Name}' is not allowed");
            }
        }
        """;

    ApplicationModelAnalysis _analysis;
    IEnumerable<ValidationRuleModel> _rules;

    void Establish()
    {
        _analysis = Analyzed.Source(Source);
        _rules = _analysis.Slice().Commands.First().Validations;
    }

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Library/Feature/Slice/Slice.cs", Source)).ShouldBeEmpty();
    [Fact] void should_leave_the_rule_without_a_message() => _rules.First(_ => _.Kind == ValidationRuleKind.NotEmpty).Message.ShouldBeNull();
    [Fact] void should_report_the_message_it_could_not_recover() => _analysis.Diagnostics.Select(_ => _.Code).ShouldContain(ScreenplayDiagnosticCodes.UnmappableValidationRule);
}
