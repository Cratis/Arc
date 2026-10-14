// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_generation_strategy_without_a_counterpart : a_generated_document
{
    [Theory]
    [InlineData("public static AuthorId New() => new(Guid.Parse(System.Security.Cryptography.RandomNumberGenerator.GetHexString(64, lowercase: true)));", "not a recognized")]
    [InlineData("public static AuthorId New() => new(GenerateValue.UuidV7(new GenerationContext(DateTimeOffset.UtcNow, GenerationContext.DefaultTenant)));", "not a recognized")]
    [InlineData("public static AuthorId New() { var id = Guid.NewGuid(); return new(id); }", "not a recognized")]
    [InlineData("public static AuthorId New() => new(Guid.Empty);", "not a recognized")]
    [InlineData("public static AuthorId New() => Create(); static AuthorId Create() => new(Guid.NewGuid());", "not a recognized")]
    [InlineData("public static AuthorId New() => Guid.NewGuid(); public static implicit operator AuthorId(Guid value) => new(Guid.Empty);", "not a recognized")]
    [InlineData("static readonly Guid context = Guid.Empty; public static AuthorId New() => new(context);", "not a recognized")]
    public void should_withhold_the_unavailable_destination_and_explain_the_unrecovered_strategy(string factory, string reason)
    {
        var source = GeneratedValueSources.With("""
            [Command] public record RegisterAuthor(string Name)
            {
                public (AuthorId, AuthorRegistered) Handle([GeneratedValue] AuthorId authorId) => (authorId, new(Name));
            }
            """).Replace("public static AuthorId New() => new(Guid.NewGuid());", factory, StringComparison.Ordinal);
        Generate((Analyzed.SlicePath, source));
        Result.Source.ShouldNotContain("generated");
        Result.Source.ShouldNotContain("returns");
        Result.Source.ShouldNotContain("produces AuthorRegistered");
        Result.Source.ShouldContain("handler");
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableEventSourceIdResult && diagnostic.Message.Contains("explicit destination depends", StringComparison.Ordinal)).ShouldBeTrue();
        var diagnostic = Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.GenerationNotRecoverable);
        diagnostic.Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Information);
        diagnostic.Message.ShouldContain(reason);
        diagnostic.Message.ShouldContain("Cratis/Screenplay#616");
        diagnostic.Location!.ShouldContain(Analyzed.SlicePath);
        AssertPartialDocument();
    }
}
