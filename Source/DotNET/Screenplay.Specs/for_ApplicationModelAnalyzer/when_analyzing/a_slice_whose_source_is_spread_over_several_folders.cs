// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// A screen is recovered from where a file sits rather than from anything the source states, so the answer is only
/// as good as the folder structure. One namespace declared in two folders is exactly the case where sitting next to
/// the source says less than usual, and saying so is the difference between a document a reader can act on and one
/// they have to double check.
/// </summary>
public class a_slice_whose_source_is_spread_over_several_folders : Specification
{
    const string Events = """
        using Cratis.Chronicle.Events;

        namespace Library.Authors.Registration;

        [EventType]
        public record AuthorRegistered(string Name);
        """;

    const string Commands = """
        using Cratis.Arc.Commands.ModelBound;

        namespace Library.Authors.Registration;

        [Command]
        public record RegisterAuthor(string Name)
        {
            public AuthorRegistered Handle() => new(Name);
        }
        """;

    static readonly (string Path, string Text)[] _sources =
    [
        ("Library/Domain/Authors/Registration/Events.cs", Events),
        ("Library/Api/Authors/Registration/Commands.cs", Commands)
    ];

    static readonly DeclaredUserInterfaceFiles _files = new(
        "Library/Api/Authors/Registration/AddAuthor.tsx",
        "Library/Domain/Authors/Registration/AuthorSummary.tsx");

    ApplicationModelAnalysis _analysis;
    IEnumerable<ScreenModel> _screens;

    void Establish()
    {
        _analysis = Analyzed.Source(_files, _sources);
        _screens = _analysis.Slice().Screens;
    }

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(_sources).ShouldBeEmpty();
    [Fact] void should_recover_a_screen_from_every_folder() => _screens.Select(_ => _.Name).ShouldContainOnly(["AddAuthor", "AuthorSummary"]);
    [Fact] void should_report_the_folders_it_took_them_from() => _analysis.Diagnostics.Select(_ => _.Code).ShouldContainOnly([ScreenplayDiagnosticCodes.AmbiguousScreenFile]);
    [Fact] void should_report_it_as_worth_knowing_rather_than_as_a_loss() => _analysis.Diagnostics.Single().Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Information);
    [Fact] void should_locate_the_report_at_the_slice() => _analysis.Diagnostics.Single().Location.ShouldEqual("Library.Authors.Registration");
    [Fact] void should_name_both_folders_in_the_report() => _analysis.Diagnostics.Single().Message.Contains("Library/Api/Authors/Registration", StringComparison.Ordinal).ShouldBeTrue();
}
