// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Analysis.Specifications;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// A command being turned down is half of what a slice is specified for, and the language says it as a rejection
/// with a reason. A source naming the reason - the constraint that held - has it carried across; a source saying
/// only that the command did not go through has no reason to carry, and a made up sentence would describe an
/// application nobody wrote. The scenario is named after the words the source uses for it, so the reason is already
/// said there.
/// </summary>
public class a_specification_expecting_a_rejection : Specification
{
    const string Slice = """
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;

        namespace Library.Authors.Registration;

        [EventType]
        public record AuthorRegistered(string Name);

        [Command]
        public record RegisterAuthor(string Name)
        {
            public AuthorRegistered Handle() => new(Name);
        }
        """;

    const string Scenario = """
        using System.Threading.Tasks;
        using Cratis.Arc.Testing.Commands;
        using Cratis.Chronicle.Testing.EventSequences;
        using Library.Authors.Registration;
        using Xunit;

        namespace Library.Authors.Registration.when_registering;

        public class and_the_name_is_taken
        {
            public const string UniqueAuthorName = "unique-author-name";

            readonly CommandScenario<RegisterAuthor> _scenario = new();
            Result _result = null!;

            async Task Because() => _result = await _scenario.Execute(new RegisterAuthor("Jane Austen"));

            [Fact] void should_not_register_the_author() => _result.ShouldHaveConstraintViolationFor(UniqueAuthorName);
        }

        public class and_nothing_was_given
        {
            readonly CommandScenario<RegisterAuthor> _scenario = new();
            Result _result = null!;

            async Task Because() => _result = await _scenario.Execute(new RegisterAuthor("Jane Austen"));

            [Fact] void should_not_succeed() => _result.ShouldNotBeSuccessful();
            [Fact] void should_say_so_on_the_result() => _result.IsSuccess.ShouldBeFalse();
        }
        """;

    static readonly (string Path, string Text)[] _sources =
    [
        ("Library/Authors/Registration/Registration.cs", Slice),
        ("Library/Authors/Registration/when_registering/and_the_name_is_taken.cs", Scenario),
        (IntegrationTesting.Path, IntegrationTesting.Source)
    ];

    ApplicationModelAnalysis _analysis;
    SpecificationModel _named;
    SpecificationModel _unnamed;

    void Establish()
    {
        _analysis = Analyzed.Source(_sources);
        var specifications = _analysis.Model.Slices.Single(_ => _.Name == "Registration").Specifications.ToList();
        _named = specifications.Single(_ => _.Name.EndsWith("and_the_name_is_taken", StringComparison.Ordinal));
        _unnamed = specifications.Single(_ => _.Name.EndsWith("and_nothing_was_given", StringComparison.Ordinal));
    }

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(_sources).ShouldBeEmpty();
    [Fact] void should_read_both_scenarios_written_in_one_file() => _analysis.Model.Slices.Single(_ => _.Name == "Registration").Specifications.Count().ShouldEqual(2);
    [Fact] void should_carry_across_the_reason_the_source_names() => _named.Errors.ShouldContainOnly(["unique-author-name"]);
    [Fact] void should_expect_no_event_alongside_a_rejection() => _named.Then.ShouldBeEmpty();
    [Fact] void should_state_a_rejection_the_source_names_no_reason_for() => _unnamed.Errors.ShouldContainOnly([string.Empty]);
    [Fact] void should_state_two_ways_of_saying_the_same_rejection_once() => _unnamed.Errors.Count().ShouldEqual(1);
    [Fact] void should_retain_rejection_step_evidence() => SpecificationEvidence.For(_named).Errors.Single().IsInSource.ShouldBeTrue();
    [Fact] void should_retain_unnamed_rejection_step_evidence() => SpecificationEvidence.For(_unnamed).Errors.Single().IsInSource.ShouldBeTrue();
    [Fact] void should_report_nothing() => _analysis.Diagnostics.ShouldBeEmpty();
}
