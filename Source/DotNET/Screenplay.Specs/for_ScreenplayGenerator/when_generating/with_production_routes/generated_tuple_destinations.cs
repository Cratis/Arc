// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_production_routes;

public class generated_tuple_destinations : a_generated_document
{
    [Theory]
    [InlineData("(string, EventForEventSourceId)", "(Name, WRAPPER)")]
    [InlineData("(EventForEventSourceId, string)", "(WRAPPER, Name)")]
    [InlineData("(string, EventForEventSourceId[])", "(Name, [WRAPPER])")]
    [InlineData("(EventForEventSourceId[], string)", "([WRAPPER], Name)")]
    [InlineData("Task<(string, EventForEventSourceId)>", "Task.FromResult((Name, WRAPPER))")]
    [InlineData("ValueTask<(EventForEventSourceId, string)>", "ValueTask.FromResult((WRAPPER, Name))")]
    [InlineData("Task<(string, EventForEventSourceId[])>", "Task.FromResult((Name, new[] { WRAPPER }))")]
    public void should_keep_the_generated_identity_and_response(string signature, string returned)
    {
        foreach (var authoring in new[] { false, true })
        {
            var expression = returned.Replace("WRAPPER", "new EventForEventSourceId(authorId, new AuthorRegistered(Name))", StringComparison.Ordinal);
            var source = "global using Cratis.Chronicle.EventSequences;\nglobal using System.Threading.Tasks;\n" + GeneratedValueSources.With($$"""
                [Command] public record RegisterAuthor(string Name)
                {
                    public {{signature}} Handle([GeneratedValue("author", Freshness = Cratis.Arc.Commands.GeneratedValueFreshness.Unguarded)] AuthorId authorId) => {{expression}};
                }
                """);
            Generate(new ScreenplayOptions { AuthoringOnlyConstructs = authoring }, (Analyzed.SlicePath, source));
            Result.Source.ShouldContain("author AuthorId generated identifier");
            Result.Source.ShouldContain("for author");
            Result.Source.ShouldContain("returns name");
            Result.Diagnostics.ShouldBeEmpty();
            AssertDocument();
        }
    }

    [Theory]
    [InlineData("(string, EventForEventSourceId, EventForEventSourceId)", "(Name, WRAPPER, AUDIT)", "")]
    [InlineData("(string, EventForEventSourceId[])", "(Name, [WRAPPER, AUDIT])", "")]
    [InlineData("(string, EventForEventSourceId[])", "(Name, [WRAPPER, AUDIT])", "[Key] AuthorId Id, ")]
    public void should_keep_a_shared_generated_destination_for_wrapper_only_siblings(string signature, string returned, string key)
    {
        foreach (var authoring in new[] { false, true })
        {
            var expression = returned
                .Replace("WRAPPER", "new EventForEventSourceId(authorId, new AuthorRegistered(Name))", StringComparison.Ordinal)
                .Replace("AUDIT", "new EventForEventSourceId(authorId, new AuthorAudited(Name))", StringComparison.Ordinal);
            var source = "global using Cratis.Chronicle.EventSequences;\n" + GeneratedValueSources.With($$"""
                [EventType] public record AuthorAudited(string Name);
                [Command] public record RegisterAuthor({{key}}string Name)
                {
                    public {{signature}} Handle([GeneratedValue("author", Freshness = Cratis.Arc.Commands.GeneratedValueFreshness.Unguarded)] AuthorId authorId) => {{expression}};
                }
                """);
            Generate(new ScreenplayOptions { AuthoringOnlyConstructs = authoring }, (Analyzed.SlicePath, source));
            var command = Result.Model.Slices.SelectMany(slice => slice.Commands).Single();
            command.Identifier.ShouldEqual(key.Length == 0 ? null : "Id");
            command.Authoring!.Identifier.ShouldEqual("author");
            command.Produces.Count().ShouldEqual(2);
            command.Produces.ShouldEachConformTo(production => production.UsesCommandContext);
            Result.Source.ShouldContain("author AuthorId generated identifier");
            Result.Source.ShouldContain("produces event AuthorRegistered");
            Result.Source.ShouldContain("produces event AuthorAudited");
            Result.Source.ShouldContain("for author");
            Result.Source.ShouldContain("returns name");
            Result.Diagnostics.ShouldBeEmpty();
            AssertDocument();
        }
    }
}
