// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// Most of the specifications an application carries stand a collaborator up behind a substitute and say what it was
/// asked to do. That is a statement about the inside of a slice rather than about its behavior, so the language has
/// nowhere to put it - and reporting each one would say a gap exists where none does. They are passed over entirely,
/// which is only safe because what is read is decided by what a specification touches rather than by where it sits.
/// </summary>
public class a_specification_of_a_collaborator : Specification
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
        using Xunit;

        namespace Library.Authors.Registration.when_registering;

        public interface INameFormatter
        {
            string Format(string name);
        }

        public class and_the_name_is_formatted
        {
            string _result = string.Empty;

            void Because() => _result = new Formatter().Format("jane austen");

            [Fact] void should_capitalize_the_name() => _result.ShouldEqualTheExpected("Jane Austen");
        }

        public class Formatter : INameFormatter
        {
            public string Format(string name) => name;
        }

        public static class Assertions
        {
            public static void ShouldEqualTheExpected(this string actual, string expected)
            {
            }
        }
        """;

    static readonly (string Path, string Text)[] _sources =
    [
        ("Library/Authors/Registration/Registration.cs", Slice),
        ("Library/Authors/Registration/when_registering/and_the_name_is_formatted.cs", Scenario),
        (IntegrationTesting.Path, IntegrationTesting.Source)
    ];

    ApplicationModelAnalysis _analysis;

    void Establish() => _analysis = Analyzed.Source(_sources);

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(_sources).ShouldBeEmpty();
    [Fact] void should_specify_the_slice_by_nothing() => _analysis.Model.Slices.Single(_ => _.Name == "Registration").Specifications.ShouldBeEmpty();
    [Fact] void should_report_no_scenario_left_out() => _analysis.Diagnostics.Where(_ => _.Code == ScreenplayDiagnosticCodes.UnreadableSpecification).ShouldBeEmpty();
}
