// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing_the_projects_of_an_application;

/// <summary>
/// Two projects declaring an artifact of one name in one slice is where "the first wins" becomes visible, and it is
/// therefore where the order the projects arrive in could decide what the document says. Nothing decides that order -
/// a solution names its projects in whatever order its file happens to list them - so the answer has to be the same
/// list either way round or the document reorders itself for reasons nobody can see.
/// </summary>
public class the_same_projects_handed_over_in_another_order : Specification
{
    const string First = """
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;

        namespace Library.Ordering.Placing;

        [EventType]
        public record OrderPlaced(string Reference);

        [Command]
        public record PlaceOrder(string Reference)
        {
            public OrderPlaced Handle() => new(Reference);
        }
        """;

    const string Second = """
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;

        namespace Library.Ordering.Placing;

        [EventType]
        public record OrderRejected(string Reference);

        [Command]
        public record PlaceOrder(string Reference)
        {
            public OrderRejected Handle() => new(Reference);
        }
        """;

    string _asGiven;
    string _reversed;

    void Because()
    {
        _asGiven = Kept(Adapter(), Application());
        _reversed = Kept(Application(), Adapter());
    }

    static Compilation Adapter() =>
        Analyzed.Project(
            "Library.Adapter",
            [],
            ("Source/Library.Adapter/Adapter.cs", "namespace Library.Adapter;"),
            ("Source/Library.Adapter/Ordering/Placing/Placing.cs", Second));

    static Compilation Application() =>
        Analyzed.Project(
            "Library",
            [],
            ("Source/Library/Program.cs", "namespace Library;"),
            ("Source/Library/Ordering/Placing/Placing.cs", First));

    static string Kept(params Compilation[] compilations) =>
        Analyzed.Projects(compilations)
            .Model.Slices.Single()
            .Commands.Single()
            .Produces.Single()
            .EventName;

    [Fact] void should_keep_a_command_at_all() => _asGiven.ShouldNotBeEmpty();
    [Fact] void should_keep_the_same_one_either_way() => _reversed.ShouldEqual(_asGiven);
}
