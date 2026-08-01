// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// A syntax tree carries a real path, which is the whole reason a screen can be recovered at all - the vertical
/// slice convention puts the component realizing a screen in the same folder as the C# it belongs to.
/// </summary>
/// <remarks>
/// Only a component is a screen. A companion carrying a second extension is tooling, a file in the folder above
/// belongs to another slice, and a file in a folder below belongs to a slice of its own.
/// </remarks>
public class a_slice_with_user_interface_files_alongside_it : Specification
{
    const string Source = """
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

    static readonly DeclaredUserInterfaceFiles _files = new(
        "Library/Authors/Registration/RegisteredAuthors.tsx",
        "Library/Authors/Registration/AddAuthor.tsx",
        "Library/Authors/Registration/AddAuthor.stories.tsx",
        "Library/Authors/Registration/AddAuthor.spec.tsx",
        "Library/Authors/Registration/Wizard/Step.tsx",
        "Library/Authors/Authors.tsx");

    ApplicationModelAnalysis _analysis;
    IEnumerable<ScreenModel> _screens;

    void Establish()
    {
        _analysis = Analyzed.Source(_files, ("Library/Authors/Registration/Registration.cs", Source));
        _screens = _analysis.Slice().Screens;
    }

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Library/Authors/Registration/Registration.cs", Source)).ShouldBeEmpty();
    [Fact] void should_recover_a_screen_per_component() => _screens.Select(_ => _.Name).ShouldContainOnly(["AddAuthor", "RegisteredAuthors"]);
    [Fact] void should_order_the_screens_by_name() => _screens.Select(_ => _.Name).ShouldEqual(["AddAuthor", "RegisteredAuthors"]);
    [Fact] void should_point_at_the_file_realizing_the_screen() => _screens.First().FilePath.ShouldEqual("Authors/Registration/AddAuthor.tsx");
    [Fact] void should_leave_out_the_companions_of_a_component() => _screens.Select(_ => _.Name).ShouldNotContain("AddAuthor.stories");
    [Fact] void should_leave_out_a_file_belonging_to_the_feature_above() => _screens.Select(_ => _.Name).ShouldNotContain("Authors");
    [Fact] void should_leave_out_a_file_belonging_to_a_folder_below() => _screens.Select(_ => _.Name).ShouldNotContain("Step");
    [Fact] void should_report_only_what_no_screen_states() => _analysis.Diagnostics.Select(_ => _.Code).Distinct().ShouldContainOnly([ScreenplayDiagnosticCodes.ScreenStructureNotInferred]);
    [Fact] void should_say_so_once_per_screen() => _analysis.Diagnostics.Count.ShouldEqual(2);
}
