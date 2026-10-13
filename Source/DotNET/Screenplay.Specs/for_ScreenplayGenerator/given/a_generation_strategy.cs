// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

/// <summary>
/// Builds an explicit generated value mapped unchanged into an event.
/// </summary>
public class a_generation_strategy : a_generated_document
{
    protected static string Source(string call, string primitive = "string") => GeneratedValueSources.With("""
        [Command] public record RegisterAuthor(string Name)
        {
            public AuthorRegistered Handle([GeneratedValue("receipt")] AuthorId value) => new(Name, value);
        }
        """)
        .Replace("public record AuthorRegistered(string Name);", "public record AuthorRegistered(string Name, AuthorId Receipt);", StringComparison.Ordinal)
        .Replace("AuthorId(Guid Value) : EventSourceId<Guid>(Value)", $"AuthorId({primitive} Value) : EventSourceId<{primitive}>(Value)", StringComparison.Ordinal)
        .Replace("new(Guid.NewGuid())", $"new({call})", StringComparison.Ordinal);

    protected void AssertStrategy(string line)
    {
        Result.Source.ShouldContain(line);
        Result.Source.ShouldContain("receipt AuthorId generated");
        Result.Source.ShouldContain("receipt = receipt");
        Result.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.GenerationNotRecoverable).ShouldBeEmpty();
        AssertDocument();
    }
}
