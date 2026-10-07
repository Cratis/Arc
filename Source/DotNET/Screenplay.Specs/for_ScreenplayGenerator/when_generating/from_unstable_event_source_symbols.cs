// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_unstable_event_source_symbols : a_generated_document
{
    [Theory]
    [InlineData("EventSourceId _first = EventSourceId.New();", "_first = EventSourceId.New();")]
    [InlineData("EventSourceId _first = EventSourceId.New();", "_first = default!;")]
    [InlineData("EventSourceId _first => EventSourceId.New();", "")]
    [InlineData("EventSourceId _first { get; set; } = EventSourceId.New();", "_first = EventSourceId.New();")]
    public void should_not_treat_a_repeated_symbol_as_a_shared_value(string declaration, string reassignment)
    {
        var source = EventAppendSources.With("_first", "_first", "_first")
            .Replace("readonly EventSourceId _first = EventSourceId.New();", declaration, StringComparison.Ordinal)
            .Replace("async Task Because() => await", "async Task Because() { " + reassignment + " await", StringComparison.Ordinal)
            .Replace(".Events(new AuthorRegistered(\"Jane Austen\"));", ".Events(new AuthorRegistered(\"Jane Austen\")); }", StringComparison.Ordinal);
        Generate((Analyzed.SlicePath, EventAppendSources.Producer),
            ("Library/Feature/Slice/when_appending/and_it_succeeds.cs", source),
            (IntegrationTesting.Path, IntegrationTesting.Source));

        Result.Source.ShouldNotContain("specification");
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification &&
            diagnostic.Message.Contains("reassigned or has a computed getter", StringComparison.Ordinal)).ShouldBeTrue();
        AssertDocument();
    }

    [Fact]
    void should_reject_a_reassigned_local()
    {
        var source = EventAppendSources.With("_first", "id", "_first")
            .Replace("async Task Because() => await", "async Task Because() { var id = EventSourceId.New(); await", StringComparison.Ordinal)
            .Replace(".Events(new AuthorRegistered(\"Jane Austen\"));", ".Events(new AuthorRegistered(\"Jane Austen\")); id = EventSourceId.New(); }", StringComparison.Ordinal);
        Generate((Analyzed.SlicePath, EventAppendSources.Producer),
            ("Library/Feature/Slice/when_appending/and_it_succeeds.cs", source),
            (IntegrationTesting.Path, IntegrationTesting.Source));

        Result.Source.ShouldNotContain("specification");
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification &&
            diagnostic.Message.Contains("event source 'id' is reassigned", StringComparison.Ordinal)).ShouldBeTrue();
        AssertDocument();
    }

    [Theory]
    [InlineData("EventSourceId _first;", "_first = EventSourceId.New();")]
    [InlineData("EventSourceId _first { get; set; }", "_first = EventSourceId.New();")]
    public void should_keep_a_value_assigned_once(string declaration, string assignment)
    {
        var source = EventAppendSources.With("_first", "_first", "_first")
            .Replace("readonly EventSourceId _first = EventSourceId.New();", declaration, StringComparison.Ordinal)
            .Replace("async Task Establish() => await", "async Task Establish() { " + assignment + " await", StringComparison.Ordinal)
            .Replace(".Events(new AuthorRegistered(\"Prior\"));", ".Events(new AuthorRegistered(\"Prior\")); }", StringComparison.Ordinal);
        Generate((Analyzed.SlicePath, EventAppendSources.Producer),
            ("Library/Feature/Slice/when_appending/and_it_succeeds.cs", source),
            (IntegrationTesting.Path, IntegrationTesting.Source));

        Result.Source.ShouldContain("specification");
        AssertDocument();
    }
}
