// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_production_routes;

public class mixed_generated_tuple_destinations : a_generated_document
{
    [Theory]
    [InlineData("(string, EventForEventSourceId, AuthorAudited)", "(Name, WRAPPER, new AuthorAudited(Name))")]
    [InlineData("(string, object[])", "(Name, [WRAPPER, new AuthorAudited(Name)])")]
    [InlineData("Task<(string, EventForEventSourceId, AuthorAudited)>", "Task.FromResult((Name, WRAPPER, new AuthorAudited(Name)))")]
    public void should_keep_plain_siblings_in_the_original_command_context(string signature, string returned)
    {
        foreach (var authoring in new[] { false, true })
        {
            Generate(new ScreenplayOptions { AuthoringOnlyConstructs = authoring }, (Analyzed.SlicePath, SourceWith(signature, returned)));
            var command = Result.Model.Slices.SelectMany(slice => slice.Commands).Single();
            command.Identifier.ShouldEqual("Id");
            command.Authoring!.Identifier.ShouldBeNull();
            command.Produces.Count().ShouldEqual(2);
            var wrapped = command.Produces.Single(production => production.EventName == "AuthorRegistered");
            wrapped.UsesCommandContext.ShouldBeFalse();
            wrapped.Destination.ShouldBeNull();
            command.Produces.Single(production => production.EventName == "AuthorAudited").UsesCommandContext.ShouldBeTrue();
            foreach (var production in command.Produces)
            {
                production.Mappings.ShouldContain(new PropertyMappingModel("Name", new PropertyPathSource("Name")));
                Result.Source.ShouldContain($"produces {production.EventName}");
            }
            AssertWithheldDestinations();
        }
    }

    [Theory]
    [InlineData("(Name, [.. Build(), WRAPPER])", "public object[] Build() => [];")]
    [InlineData("(Name, [Build(), WRAPPER])", "public AuthorAudited Build() => new(Name);")]
    public void should_refuse_generated_fallback_for_ambiguous_siblings(string returned, string helper)
    {
        foreach (var authoring in new[] { false, true })
        {
            Generate(new ScreenplayOptions { AuthoringOnlyConstructs = authoring }, (Analyzed.SlicePath, SourceWith("(string, object[])", returned, helper)));
            var command = Result.Model.Slices.SelectMany(slice => slice.Commands).Single();
            command.Identifier.ShouldEqual("Id");
            command.Authoring!.Identifier.ShouldBeNull();
            command.Produces.Single().EventName.ShouldEqual("AuthorRegistered");
            Result.Source.ShouldContain("produces AuthorRegistered");
            AssertWithheldDestinations();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_refuse_a_shared_identity_for_distinct_generated_wrappers(bool authoring)
    {
        var source = SourceWith(
            "(string, EventForEventSourceId, EventForEventSourceId)",
            "(Name, WRAPPER, new EventForEventSourceId(auditId, new AuthorAudited(Name)))",
            additionalParameter: ", [GeneratedValue(\"audit\", Freshness = Cratis.Arc.Commands.GeneratedValueFreshness.Unguarded)] AuthorId auditId");
        Generate(new ScreenplayOptions { AuthoringOnlyConstructs = authoring }, (Analyzed.SlicePath, source));
        var command = Result.Model.Slices.SelectMany(slice => slice.Commands).Single();
        command.Identifier.ShouldEqual("Id");
        command.Authoring!.Identifier.ShouldBeNull();
        command.Produces.Count().ShouldEqual(2);
        command.Produces.ShouldEachConformTo(production => !production.UsesCommandContext && production.Destination is null);
        Result.Source.ShouldContain("produces AuthorRegistered");
        Result.Source.ShouldContain("produces AuthorAudited");
        Result.Source.ShouldNotContain("for audit");
        AssertWithheldDestinations();
    }

    static string SourceWith(string signature, string returned, string helper = "", string additionalParameter = "")
    {
        var expression = returned.Replace("WRAPPER", "new EventForEventSourceId(authorId, new AuthorRegistered(Name))", StringComparison.Ordinal);
        return "global using Cratis.Chronicle.EventSequences;\nglobal using System.Threading.Tasks;\n" + GeneratedValueSources.With($$"""
            [EventType] public record AuthorAudited(string Name);
            [Command] public record RegisterAuthor([Key] AuthorId Id, string Name)
            {
                public {{signature}} Handle([GeneratedValue("author", Freshness = Cratis.Arc.Commands.GeneratedValueFreshness.Unguarded)] AuthorId authorId{{additionalParameter}}) => {{expression}};
                {{helper}}
            }
            """);
    }

    void AssertWithheldDestinations()
    {
        Result.Source.ShouldNotContain("for author");
        Result.Source.ShouldNotContain("for id");
        Result.Source.ShouldNotContain(" generated identifier");
        Result.Diagnostics.Count(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnrepresentableProductionDestination).ShouldEqual(1);
        AssertDocument();
    }
}
