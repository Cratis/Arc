// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_production_routes;

public class a_reference_execution : a_generated_document
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_execute_each_ordered_event_at_its_own_destination_and_effective_route(bool authoring)
    {
        var source = ProductionRouteSources.With($$"""
            public object[] Handle() => [
                new AuthorRegistered(Name),
                {{ProductionRouteSources.Wrapper}},
                new EventForEventSourceId(OtherId, new AuthorRegistered(Name)) { EventSourceType = "Ledger", EventStreamType = "Audit" }
            ];
            """);
        Generate(new ScreenplayOptions { AuthoringOnlyConstructs = authoring }, (Analyzed.SlicePath, source));
        AssertDocument();
        var command = Bound.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single().Commands.Single();
        command.Route.ShouldNotBeNull();
        command.Produces.Length.ShouldEqual(3);
        command.Produces[0].Route.ShouldBeNull();
        command.Produces[1].Route.ShouldNotBeNull();
        command.Produces[2].Route.ShouldNotBeNull();
        const string Scenario = "\n" + """
                  specification Routing
                    when RegisterAuthor
                      id = "account-1"
                      otherId = "ledger-2"
                      month = "2026-10"
                      name = "Jane"
                    then AuthorRegistered
                      for "account-1"
                      stream Account.Main
                      name = "Jane"
                    then AuthorRegistered
                      for "ledger-2"
                      stream Ledger.Postings
                        streamId = "2026-10"
                      name = "Jane"
                    then AuthorRegistered
                      for "ledger-2"
                      stream Ledger.Audit
                      name = "Jane"
            """;
        const string Key = "application";
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Library"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument(Key), Key, "application.play", Result.Source + Scenario);
        Bound = new SemanticModelCompiler().Compile("Library", SemanticDocumentSet.Create([document], catalog));
        Assert.True(Bound.Success, string.Join(Environment.NewLine, Bound.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")));
        Run("Routing").Passed.ShouldBeTrue();
    }
}
