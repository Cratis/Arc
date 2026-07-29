// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// A rule carries no condition of its own, so a rule held to one is stated as though it always holds. That is a real
/// difference between the document and the application rather than a rule that could not be read, and a report saying
/// only that a call had no counterpart leaves a reader unable to tell which. Writing the condition into it is what
/// makes the difference something they can weigh.
/// </summary>
public class a_validator_holding_rules_to_a_condition : Specification
{
    const string Source = """
        using Cratis.Arc.Commands;
        using Cratis.Arc.Commands.ModelBound;
        using FluentValidation;

        namespace Library.Lending.Reserving;

        [Command]
        public record ReserveBook(string Isbn, string Note, bool IsRush)
        {
            public void Handle()
            {
            }
        }

        public class ReserveBookValidator : CommandValidator<ReserveBook>
        {
            public ReserveBookValidator()
            {
                RuleFor(_ => _.Isbn).NotEmpty().When(command => command.IsRush);
                RuleFor(_ => _.Note).MaximumLength(50).Unless(command => command.IsRush);
            }
        }
        """;

    ApplicationModelAnalysis _analysis;

    void Establish() => _analysis = Analyzed.Source(Source);

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Library/Feature/Slice/Slice.cs", Source)).ShouldBeEmpty();
    [Fact] void should_still_state_the_rules_the_condition_held() => _analysis.Slice().Commands.First().Validations.Select(_ => _.Property).ShouldContainOnly(["Isbn", "Note"]);
    [Fact] void should_report_one_condition_for_each_chain() => _analysis.Diagnostics.Count.ShouldEqual(2);
    [Fact] void should_report_them_as_rules_it_could_not_express() => _analysis.Diagnostics.Select(_ => _.Code).Distinct(StringComparer.Ordinal).ShouldContainOnly([ScreenplayDiagnosticCodes.UnmappableValidationRule]);
    [Fact] void should_say_what_the_rules_were_held_to() => _analysis.Diagnostics.Any(_ => _.Message.Contains("When(command => command.IsRush)", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_say_the_same_of_a_condition_written_the_other_way_round() => _analysis.Diagnostics.Any(_ => _.Message.Contains("Unless(command => command.IsRush)", StringComparison.Ordinal)).ShouldBeTrue();
}
