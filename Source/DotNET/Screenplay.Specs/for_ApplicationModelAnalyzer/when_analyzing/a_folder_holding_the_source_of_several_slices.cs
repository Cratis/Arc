// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// A slice is a namespace and a screen is a folder, so two namespaces in one folder leave a component that could
/// belong to either. Both slices are told they end in it, because leaving it out of one of them would be a guess,
/// and the guess is reported instead.
/// </summary>
public class a_folder_holding_the_source_of_several_slices : Specification
{
    const string Source = """
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;

        namespace Library.Authors.Registration
        {
            [EventType]
            public record AuthorRegistered(string Name);

            [Command]
            public record RegisterAuthor(string Name)
            {
                public AuthorRegistered Handle() => new(Name);
            }
        }

        namespace Library.Authors.Retirement
        {
            [EventType]
            public record AuthorRetired(string Name);

            [Command]
            public record RetireAuthor(string Name)
            {
                public AuthorRetired Handle() => new(Name);
            }
        }
        """;

    static readonly DeclaredUserInterfaceFiles _files = new("Library/Authors/Authors.tsx");

    ApplicationModelAnalysis _analysis;
    IReadOnlyList<ScreenplayDiagnostic> _shared;

    void Establish()
    {
        _analysis = Analyzed.Source(_files, ("Library/Authors/Authors.cs", Source));
        _shared = [.. _analysis.Diagnostics.Where(_ => _.Code == ScreenplayDiagnosticCodes.AmbiguousScreenFile)];
    }

    IEnumerable<ScreenModel> ScreensOf(string @namespace) =>
        _analysis.Model.Slices.Single(_ => _.Namespace == @namespace).Screens;

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Library/Authors/Authors.cs", Source)).ShouldBeEmpty();
    [Fact] void should_end_the_first_slice_in_the_screen() => ScreensOf("Library.Authors.Registration").Select(_ => _.Name).ShouldContainOnly(["Authors"]);
    [Fact] void should_end_the_second_slice_in_the_screen_as_well() => ScreensOf("Library.Authors.Retirement").Select(_ => _.Name).ShouldContainOnly(["Authors"]);
    [Fact] void should_report_the_folder_as_shared() => _shared.ShouldNotBeEmpty();
    [Fact] void should_report_it_once() => _shared.Count.ShouldEqual(1);
    [Fact] void should_locate_the_report_at_the_slice_that_found_it_taken() => _shared[0].Location.ShouldEqual("Library.Authors.Retirement");
    [Fact] void should_name_the_slice_that_claimed_it_first() => _shared[0].Message.Contains("Library.Authors.Registration", StringComparison.Ordinal).ShouldBeTrue();
}
