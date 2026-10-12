// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// A scenario in another slice still needs the executed command's caller.
/// </summary>
public class from_an_authorized_caller_scenario_in_another_slice : a_generated_document
{
    /// <summary>
    /// Generates an authorized command and a caller-less scenario in another slice.
    /// </summary>
    void Because()
    {
        var sources = CallerScenarioSources.With("").Select(source => (source.Path, source.Text.Replace(
            "namespace Library.Authors.Registration.when_registering;",
            "using Library.Authors.Registration; namespace Library.Authors.Checking.when_registering;",
            StringComparison.Ordinal))).ToArray();
        Generate([
            .. sources,
            ("Library/Authors/Checking/CheckAuthor.cs", """
                using Cratis.Arc.Commands.ModelBound;
                using Cratis.Chronicle.Events;
                namespace Library.Authors.Checking;
                [EventType] public record AuthorChecked(string Name);
                [Command] public record CheckAuthor(string Name)
                {
                    public AuthorChecked Handle() => new(Name);
                }
                """)
        ]);
    }

    /// <summary>
    /// The command's authorization gate withholds the caller-less scenario.
    /// </summary>
    [Fact] void should_withhold_the_scenario_without_a_caller() => Result.Source.ShouldNotContain($"specification {CallerScenarioSources.ScenarioName}");

    /// <summary>
    /// The omission names its missing caller.
    /// </summary>
    [Fact] void should_report_the_missing_caller() => Result.Diagnostics.Single(diagnostic => diagnostic.Code == "SP0039").Message.ShouldContain("no caller was recovered");

    /// <summary>
    /// Removing the caller-less scenario keeps the document valid.
    /// </summary>
    [Fact] void should_keep_the_document_valid() => AssertDocument();
}
