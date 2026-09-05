// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Emission;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// A generated document is meant to be committed, diffed and reviewed, which it cannot be if it reorders itself
/// between builds. Nothing decides the order a build hands its source files over in, so the same files in another
/// order have to produce the same bytes - that is the whole of the determinism promise, stated as something that
/// can fail.
/// </summary>
public class from_the_same_source_handed_over_in_another_order : Specification
{
    static readonly (string Path, string Text)[] _sources =
    [
        ("Library/Authors/Registration/Registration.cs", LibrarySource.AuthorRegistration),
        ("Library/Authors/Listing/Listing.cs", LibrarySource.AuthorListing),
        ("Library/Lending/Reserving/Reserving.cs", LibrarySource.Reserving),
        ("Library/Lending/Notifications/Notifications.cs", LibrarySource.Notifications)
    ];

    string _asGiven;
    string _reversed;

    void Because()
    {
        _asGiven = Generate(_sources);
        _reversed = Generate([.. _sources.AsEnumerable().Reverse()]);
    }

    static string Generate((string Path, string Text)[] sources) =>
        new ScreenplayGenerator(
                new ApplicationModelAnalyzer(DeclaredUserInterfaceFiles.None),
                new ScreenplayEmitter())
            .Generate(Analyzed.Compile(sources), new ScreenplayOptions())
            .Source;

    [Fact] void should_have_produced_a_document_at_all() => _asGiven.ShouldNotBeEmpty();
    [Fact] void should_produce_the_same_document_either_way() => _reversed.ShouldEqual(_asGiven);
}
