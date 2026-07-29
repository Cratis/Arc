// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// A source generator writes partial members into a slice's namespace and emits them to disk under <c>obj/</c>. Those
/// files contribute symbols to the slice but say nothing about where its source - and therefore its screens - live, so
/// the slice is not spread over the folder they sit in and screen discovery must not scan it.
/// </summary>
public class a_slice_whose_generated_members_sit_under_obj : Specification
{
    const string Source = """
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;

        namespace Library.Authors.Registration;

        [EventType]
        public record AuthorRegistered(string Name);

        [Command]
        public partial record RegisterAuthor(string Name)
        {
            public AuthorRegistered Handle() => new(Name);
        }
        """;

    const string Generated = """
        namespace Library.Authors.Registration;

        public partial record RegisterAuthor
        {
        }
        """;

    static readonly DeclaredUserInterfaceFiles _files = new(
        "Library/Authors/Registration/AddAuthor.tsx");

    ApplicationModelAnalysis _analysis;
    IEnumerable<ScreenModel> _screens;

    void Establish()
    {
        _analysis = Analyzed.Source(
            _files,
            ("Library/Authors/Registration/Registration.cs", Source),
            ("Library/obj/Debug/net10.0/Generator/RegisterAuthor.Logging.g.cs", Generated));
        _screens = _analysis.Slice().Screens;
    }

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Library/Authors/Registration/Registration.cs", Source), ("Library/obj/Debug/net10.0/Generator/RegisterAuthor.Logging.g.cs", Generated)).ShouldBeEmpty();
    [Fact] void should_not_report_the_slice_as_spread_over_folders() => _analysis.Diagnostics.Select(_ => _.Code).ShouldNotContain(ScreenplayDiagnosticCodes.AmbiguousScreenFile);
    [Fact] void should_still_recover_the_screen_from_the_authored_folder() => _screens.Select(_ => _.Name).ShouldContainOnly(["AddAuthor"]);
    [Fact] void should_point_at_the_file_realizing_the_screen() => _screens.Single().FilePath.ShouldEqual("Authors/Registration/AddAuthor.tsx");
}
