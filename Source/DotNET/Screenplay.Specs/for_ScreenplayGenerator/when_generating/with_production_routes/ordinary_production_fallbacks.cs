// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_production_routes;

public class ordinary_production_fallbacks : a_generated_document
{
    [Theory]
    [InlineData("public AuthorRegistered Handle() => new(Name);")]
    [InlineData("public Task<AppendResult> Handle(IEventLog log) => log.Append(Id, new AuthorRegistered(Name));")]
    public void should_preserve_ordinary_event_and_append_productions(string handler)
    {
        foreach (var authoring in new[] { false, true })
        {
            var source = IdentifierSources.With($$"""
                [Command] public record RegisterAuthor([Key] Guid Id, string Name)
                {
                    {{handler}}
                }
                """).Replace("using System;", "using System;\nusing System.Threading.Tasks;\nusing Cratis.Chronicle.EventSequences;", StringComparison.Ordinal);
            Analyzed.ErrorsIn(Analyzed.Compile([(Analyzed.SlicePath, source)])).ShouldBeEmpty();
            Generate(new ScreenplayOptions { AuthoringOnlyConstructs = authoring }, (Analyzed.SlicePath, source));
            Result.Model.Slices.SelectMany(slice => slice.Commands).Single().Produces.Single().EventName.ShouldEqual("AuthorRegistered");
            (Result.Source.Contains("produces AuthorRegistered", StringComparison.Ordinal) ||
                Result.Source.Contains("produces event AuthorRegistered", StringComparison.Ordinal)).ShouldBeTrue();
            Result.Source.ShouldNotContain("handler");
            Result.Diagnostics.Where(diagnostic => diagnostic.Message.Contains("no production of the command is stated", StringComparison.Ordinal)).ShouldBeEmpty();
            AssertDocument();
        }
    }
}
