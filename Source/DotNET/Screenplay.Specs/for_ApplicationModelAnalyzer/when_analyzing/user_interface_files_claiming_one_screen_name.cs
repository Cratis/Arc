// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// A name is what one screen is told apart from another by, and what <c>navigate to</c> refers to, so a slice
/// declaring the same one twice means it differently in two places. The first is kept and the rest are reported,
/// because a document that says one thing is better than one that says two and settles neither.
/// </summary>
public class user_interface_files_claiming_one_screen_name : Specification
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
        ("Library/Api/Authors/Registration/Commands.cs", Commands),
        ("Library/Domain/Authors/Registration/Events.cs", Events)
    ];

    static readonly DeclaredUserInterfaceFiles _files = new(
        "Library/Domain/Authors/Registration/AddAuthor.tsx",
        "Library/Api/Authors/Registration/AddAuthor.tsx");

    ApplicationModelAnalysis _analysis;
    IEnumerable<ScreenModel> _screens;
    ScreenplayDiagnostic _repeated;

    void Establish()
    {
        _analysis = Analyzed.Source(_files, _sources);
        _screens = _analysis.Slice().Screens;
        _repeated = _analysis.Diagnostics.Single(_ => _.Severity == ScreenplayDiagnosticSeverity.Warning);
    }

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(_sources).ShouldBeEmpty();
    [Fact] void should_declare_the_screen_once() => _screens.Count().ShouldEqual(1);
    [Fact] void should_keep_the_first_file_it_read() => _screens.Single().FilePath.ShouldEqual("Api/Authors/Registration/AddAuthor.tsx");
    [Fact] void should_report_the_file_it_left_out() => _repeated.Code.ShouldEqual(ScreenplayDiagnosticCodes.AmbiguousScreenFile);
    [Fact] void should_name_the_file_it_left_out() => _repeated.Message.Contains("Domain/Authors/Registration/AddAuthor.tsx", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_locate_the_report_at_the_slice() => _repeated.Location.ShouldEqual("Library.Authors.Registration");
}
