// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Emission;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// The same files in another order already have to print the same bytes. With several projects there is a second
/// order nobody decides - the one a host enumerates the projects of a solution in - and a document that reordered
/// itself with it would be exactly as impossible to commit, diff and review. This is that promise, stated as
/// something that can fail.
/// </summary>
public class from_the_same_projects_handed_over_in_another_order : Specification
{
    string _asGiven;
    string _reversed;

    void Because()
    {
        _asGiven = Generate(contracts => [LayeredSource.ApplicationProject(contracts), contracts]);
        _reversed = Generate(contracts => [contracts, LayeredSource.ApplicationProject(contracts)]);
    }

    static string Generate(Func<Compilation, IReadOnlyList<Compilation>> arrange) =>
        new ScreenplayGenerator(
                new ApplicationModelAnalyzer(DeclaredUserInterfaceFiles.None),
                new ScreenplayEmitter())
            .Generate(arrange(LayeredSource.ContractsProject()), new ScreenplayOptions())
            .Source;

    [Fact] void should_have_produced_a_document_at_all() => _asGiven.ShouldNotBeEmpty();
    [Fact] void should_produce_the_same_document_either_way() => _reversed.ShouldEqual(_asGiven);
}
