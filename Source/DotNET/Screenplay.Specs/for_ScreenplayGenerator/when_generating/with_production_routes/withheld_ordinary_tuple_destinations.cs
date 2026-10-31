// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_production_routes;

public class withheld_ordinary_tuple_destinations : a_production_route_document
{
    const string CommandPath = "Banking/Authors/Registration/RegisterAuthor.cs";
    static readonly string _source = TupleDestinationSources.With("""
        public (string, EventForEventSourceId, EventForEventSourceId) Handle() => (Name, new EventForEventSourceId(new EventSourceId((OtherId).ToString()), new AuthorRegistered(Name)), new(OtherId + "computed", new AuthorAudited(Name)));
        """).Replace("namespace Library.Authors.Registration;", "namespace Banking.Authors.Registration;", StringComparison.Ordinal);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_withhold_both_ordinary_productions_and_preserve_the_native_handler(bool authoring)
    {
        GenerateOrdinary(_source, authoring);
        AssertWithheld();
    }

    [Theory]
    [InlineData("public (string, EventForEventSourceId, EventForEventSourceId) Handle() => (Name, WRAPPER, new(OtherId + \"computed\", new AuthorAudited(Name)));")]
    [InlineData("public (string, EventForEventSourceId, EventForEventSourceId) Handle() => (Name, WRAPPER, new(OtherId, new AuthorAudited(Name)) { Subject = \"someone\" });")]
    [InlineData("public (string, EventForEventSourceId, EventForEventSourceId) Handle() => (Name, WRAPPER, new(OtherId, new AuthorAudited(Name)) { Tags = [\"tag\"] });")]
    [InlineData("public (string, EventForEventSourceId, EventForEventSourceId) Handle() => (Name, WRAPPER, new(OtherId, new AuthorAudited(Name)) { Occurred = DateTimeOffset.UtcNow });")]
    [InlineData("public (string, EventForEventSourceId, EventForEventSourceId) Handle() => (Name, WRAPPER, new(OtherId, new AuthorAudited(Name)) { EventStreamId = Month + \"computed\" });")]
    [InlineData("public (string, EventForEventSourceId, EventForEventSourceId) Handle() => (Name, WRAPPER, new(OtherId, new AuthorAudited(Name)) { EventSource = typeof(LedgerEventSource), EventStream = \"Unknown\" });")]
    public void should_withhold_the_whole_supported_tuple_with_an_unsafe_sibling(string handler)
    {
        foreach (var authoring in new[] { false, true })
        {
            var source = TupleDestinationSources.With(handler.Replace("WRAPPER", TupleDestinationSources.RoutedWrapper, StringComparison.Ordinal))
                .Replace("namespace Library.Authors.Registration;", "namespace Banking.Authors.Registration;", StringComparison.Ordinal);
            GenerateOrdinary(source, authoring);
            AssertWithheld();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_keep_both_ordinary_productions_with_supported_destinations(bool authoring)
    {
        GenerateOrdinary(_source.Replace("OtherId + \"computed\"", "OtherId", StringComparison.Ordinal), authoring);
        var command = Result.Model.Slices.SelectMany(slice => slice.Commands).Single();
        command.Produces.Count().ShouldEqual(2);
        command.Produces.ShouldEachConformTo(production => production.Destination is not null && production.Route is not null);
        Result.Source.ShouldContain("produces event AuthorRegistered");
        Result.Source.ShouldContain("produces event AuthorAudited");
        Result.Source.ShouldContain("for otherId");
        Result.Source.ShouldContain("stream Account.Main");
        Result.Source.ShouldNotContain("handler");
        Result.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnrepresentableProductionDestination).ShouldBeEmpty();
        AssertRoutedDocument();
    }

    void AssertWithheld()
    {
        var command = Result.Model.Slices.SelectMany(slice => slice.Commands).Single();
        command.Name.ShouldEqual("RegisterAuthor");
        command.Produces.ShouldBeEmpty();
        Result.Source.ShouldNotContain("produces");
        Result.Source.ShouldContain("handler");
        Result.Source.ShouldContain("Authors/Registration/RegisterAuthor.cs");
        var diagnostic = Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnrepresentableProductionDestination);
        diagnostic.Message.ShouldContain("no production of the command is stated");
        AssertPartialDocument();
        Bound.Diagnostics.Where(diagnostic => diagnostic.Severity == Cratis.Screenplay.Diagnostics.DiagnosticSeverity.Error).Select(diagnostic => diagnostic.Code).ShouldEqual(["PLAY0800"]);
    }

    void GenerateOrdinary(string source, bool authoring)
    {
        (string Path, string Text)[] sources = [("Banking/Program.cs", "namespace Banking;"), (CommandPath, source)];
        Analyzed.ErrorsIn(Analyzed.Compile(sources)).ShouldBeEmpty();
        Generate(new ScreenplayOptions { Domain = "Banking", Module = "Banking", AuthoringOnlyConstructs = authoring }, sources);
    }
}
