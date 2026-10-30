// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_production_routes;

public class withheld_generated_tuple_destinations : a_generated_document
{
    const string Registered = "new EventForEventSourceId(authorId, new AuthorRegistered(Name))";
    const string Generated = ", [GeneratedValue(\"audit\", Freshness = Cratis.Arc.Commands.GeneratedValueFreshness.Unguarded)] AuthorId auditId";

    [Theory]
    [InlineData("(string, EventForEventSourceId, AuthorAudited)", "(Name, WRAPPER, new AuthorAudited(Name))", "", "")]
    [InlineData("(string, object[])", "(Name, [WRAPPER, new AuthorAudited(Name)])", "", "")]
    [InlineData("Task<(string, EventForEventSourceId, AuthorAudited)>", "Task.FromResult((Name, WRAPPER, new AuthorAudited(Name)))", "", "")]
    [InlineData("(string, object[])", "(Name, [.. Build(), WRAPPER])", "public object[] Build() => [];", "")]
    [InlineData("(string, object[])", "(Name, [Build(), WRAPPER])", "public AuthorAudited Build() => new(Name);", "")]
    [InlineData("(string, EventForEventSourceId, EventForEventSourceId)", "(Name, WRAPPER, new EventForEventSourceId(auditId, new AuthorAudited(Name)))", "", Generated)]
    public void should_withhold_every_production_of_an_unrepresentable_generated_tuple(string signature, string returned, string helper, string additionalParameter)
    {
        foreach (var authoring in new[] { false, true })
        {
            var expression = returned.Replace("WRAPPER", Registered, StringComparison.Ordinal);
            var source = "global using Cratis.Chronicle.EventSequences;\nglobal using System.Threading.Tasks;\n" + GeneratedValueSources.With($$"""
                [EventType] public record AuthorAudited(string Name);
                [Command] public record RegisterAuthor([Key] AuthorId Id, string Name)
                {
                    public {{signature}} Handle([GeneratedValue("author", Freshness = Cratis.Arc.Commands.GeneratedValueFreshness.Unguarded)] AuthorId authorId{{additionalParameter}}) => {{expression}};
                    {{helper}}
                }
                """);
            Generate(new ScreenplayOptions { AuthoringOnlyConstructs = authoring }, (Analyzed.SlicePath, source));
            var command = Result.Model.Slices.SelectMany(slice => slice.Commands).Single();
            command.Produces.ShouldBeEmpty();
            command.Authoring.Identifier.ShouldBeNull();
            Result.Source.ShouldNotContain("for author");
            Result.Source.ShouldNotContain("for audit");
            Result.Source.ShouldNotContain("for id");
            Result.Source.ShouldNotContain(" generated identifier");
            Result.Diagnostics.Count(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnrepresentableProductionDestination).ShouldEqual(1);
            AssertPartialDocument();
        }
    }
}
