// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_production_routes;

public class effective_metadata : a_production_route_document
{
    [Theory]
    [InlineData("EventSourceType = \"Ledger\", EventStreamType = \"Postings\", EventStreamId = Month", "Ledger.Postings", "month")]
    [InlineData("EventSourceType = \"ledger\", EventStreamType = \"postings\", EventStreamId = Month", "ledger.postings", "month")]
    [InlineData("EventSourceType = \"Ledger\", EventStreamType = \"Postings\", EventStreamId = \"2026-10\"", "Ledger.Postings", "\"2026-10\"")]
    [InlineData("EventSourceType = \"Ledger\", EventStreamType = \"Postings\", EventStreamId = \"{Month}\"", "Ledger.Postings", "\"{Month}\"")]
    [InlineData("EventSource = typeof(LedgerEventSource), EventStream = LedgerEventSource.Postings, EventStreamId = Month", "Ledger.Postings", "month")]
    [InlineData("EventSourceType = \"stored-ledger\", EventStreamType = \"stored-postings\", EventStreamId = Month", "Ledger.Postings", "month")]
    [InlineData("EventSourceType = \"Default\", EventStreamType = \"All\", EventStreamId = \"Default\"", "Account.Main", "month")]
    public void should_resolve_complete_routes_and_preserve_literal_wrapper_text(string metadata, string route, string key)
    {
        foreach (var authoring in new[] { false, true })
        {
            var source = ProductionRouteSources.With($"public EventForEventSourceId Handle() => new(OtherId, new AuthorRegistered(Name)) {{ {metadata} }};", "EventSourceType(\"Account\"), EventStreamType(\"Main\"), EventStreamId(\"{Month}\")");
            Generate(new ScreenplayOptions { AuthoringOnlyConstructs = authoring }, (Analyzed.SlicePath, source));
            Result.Source.ShouldContain($"stream {route}");
            Result.Source.ShouldContain($"streamId = {key}");
            Result.Source.ShouldContain("for otherId");
            if (metadata.Contains("typeof", StringComparison.Ordinal) || metadata.Contains("stored-ledger", StringComparison.Ordinal))
            {
                Result.Source.ShouldContain("id \"stored-ledger\"");
                Result.Source.ShouldContain("id \"stored-postings\"");
            }
            AssertRoutedDocument();
        }
    }

    [Fact]
    void should_resolve_a_definition_named_by_convention_without_a_source_attribute()
    {
        var source = ProductionRouteSources.With("public EventForEventSourceId Handle() => new(OtherId, new AuthorRegistered(Name)) { EventSource = typeof(LedgerEventSource), EventStream = LedgerEventSource.Postings };")
            .Replace("[Cratis.Chronicle.EventSources.EventSource(\"stored-ledger\")]", string.Empty, StringComparison.Ordinal);
        Generate((Analyzed.SlicePath, source));
        Result.Source.ShouldContain("stream Ledger.Postings");
        Result.Source.ShouldContain("id \"stored-postings\"");
        AssertDocument();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_emit_a_route_without_a_default_command_route(bool authoring)
    {
        Generate(new ScreenplayOptions { AuthoringOnlyConstructs = authoring }, (Analyzed.SlicePath, ProductionRouteSources.With($"public EventForEventSourceId Handle() => {ProductionRouteSources.Wrapper};", string.Empty)));
        Result.Source.ShouldContain("stream Ledger.Postings");
        Result.Source.ShouldNotContain("stream Account.Main");
        Result.Source.ShouldContain("for otherId");
        AssertDocument();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_bind_named_arguments_by_constructor_parameter_not_written_order(bool authoring)
    {
        Generate(new ScreenplayOptions { AuthoringOnlyConstructs = authoring }, (Analyzed.SlicePath, ProductionRouteSources.With("public EventForEventSourceId Handle() => new(Event: new AuthorRegistered(Name), EventSourceId: OtherId) { EventStreamType = \"Postings\", EventStreamId = Month };")));
        Result.Source.ShouldContain("for otherId");
        Result.Source.ShouldContain("stream Account.Postings");
        Result.Source.ShouldContain("streamId = month");
        AssertDocument();
    }
}
