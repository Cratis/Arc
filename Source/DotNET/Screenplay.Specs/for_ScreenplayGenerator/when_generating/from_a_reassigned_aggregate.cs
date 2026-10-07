// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_reassigned_aggregate : a_generated_document
{
    [Theory]
    [InlineData("author = await factory.Get<Author>(OtherId);")]
    [InlineData("Reset(ref author);")]
    [InlineData("ResetOut(out author);")]
    [InlineData("Action change = () => author = new Author();")]
    [InlineData("ref Author alias = ref author;")]
    public void should_not_trust_a_changed_parameter(string change) => AssertLegacy(null, change);

    [Theory]
    [InlineData("author = await factory.Get<Author>(OtherId);")]
    [InlineData("Reset(ref author);")]
    [InlineData("ResetOut(out author);")]
    [InlineData("Action change = () => author = new Author();")]
    [InlineData("ref Author alias = ref author;")]
    public void should_not_trust_a_changed_local(string change) => AssertLegacy("var author = await factory.Get<Author>(Id);", change);

    void AssertLegacy(string? declaration, string change)
    {
        var parameter = declaration is null ? "Author author, " : string.Empty;
        var source = AggregateRoutingSources.With($$"""
            static void Reset(ref Author author) => author = new Author();
            static void ResetOut(out Author author) => author = new Author();
            public async Task Handle({{parameter}}IAggregateRootFactory factory)
            {
                {{declaration}}
                {{change}}
                await author.Register(Name);
            }
            """);
        Generate((Analyzed.SlicePath, source));
        Result.Source.ShouldContain("produces AuthorRegistered");
        Result.Source.ShouldNotContain("produces event AuthorRegistered");
        Result.Source.ShouldNotContain("identifier");
        Result.Source.ShouldNotContain("for id");
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnrepresentableProductionDestination).ShouldBeTrue();
        AssertDocument();
    }
}
