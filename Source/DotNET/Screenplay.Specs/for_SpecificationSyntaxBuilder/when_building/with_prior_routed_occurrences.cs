// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Emission.Specifications;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Arc.Screenplay.for_SpecificationSyntaxBuilder.when_building;

public class with_prior_routed_occurrences : Specification
{
    List<SpecificationSyntax> _syntax;
    readonly List<SpecificationStateModel> _occurrences = [];
    readonly ScreenplayDiagnostics _diagnostics = new();

    void Because()
    {
        var command = new CommandModel("RegisterAuthor", null, [new("Id", new("Uuid", false, false))], null, [], [new("AuthorRegistered", null, []) { UsesCommandContext = true }], null, null) { Identifier = "Id" };
        var slice = SliceModel.Empty("Library.Authors.Registration", "Registration", SliceKind.StateChange) with { Commands = [command] };
        var application = new ApplicationModel("Library", "Library", [], [], [slice], []);
        var first = new SpecificationStateModel("AuthorRegistered", SpecificationStateKind.Event, [])
        {
            For = new("55555555-5555-5555-5555-555555555555"),
            Route = new("Account", "Registrations", null)
        };
        var second = new SpecificationStateModel("ExternalFact", SpecificationStateKind.Event, [])
        {
            For = new("author"),
            Route = new("Account", "External", null)
        };
        var assertion = new SpecificationStateModel("Author", SpecificationStateKind.ReadModel, []);
        _syntax = new SpecificationSyntaxBuilder(new ScreenplayNaming())
        {
            Application = application,
            ExecutableRoutes = true,
            Diagnostics = _diagnostics,
            RoutedOccurrences = _occurrences
        }.Build([new("First", [], first, [assertion], []), new("Second", [], second, [assertion], [])]).ToList();
    }

    [Fact] void should_keep_the_first_scenario() => _syntax.Select(syntax => syntax.Name).ShouldContainOnly(["First"]);
    [Fact] void should_reject_the_conflicting_source_type_in_the_second_scenario() => _diagnostics.All.ShouldContain(diagnostic => diagnostic.Message.Contains("Second", StringComparison.Ordinal) && diagnostic.Message.Contains("route cannot be stated", StringComparison.Ordinal));
    [Fact] void should_collect_only_the_retained_occurrence() => _occurrences.Select(occurrence => occurrence.Name).ShouldContainOnly(["AuthorRegistered"]);
}
