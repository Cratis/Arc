// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_tuples_with_unproven_event_destinations : a_generated_document
{
    [Theory]
    [InlineData("public (AuthorRegistered, AuthorId) Handle() => (new(Name), new AuthorId(Id));")]
    [InlineData("public (AuthorRegistered, object) Handle() => (new(Name), new AuthorId(Id));")]
    [InlineData("public (AuthorRegistered, object) Handle() => (new(Name), Name);")]
    [InlineData("public (AuthorRegistered, IComparable) Handle() => (new(Name), Name);")]
    [InlineData("public void Handle() { var unused = (new AuthorRegistered(Name), Name); }")]
    [InlineData("public async System.Threading.Tasks.Task Handle(Cratis.Chronicle.EventSequences.IEventLog log) { (AuthorRegistered, string) Construct() => (new(Name), Name); await log.Append(OtherId, Construct().Item1); }")]
    public void should_not_claim_the_command_identity_for_an_unproven_tuple(string handler)
    {
        var source = IdentifierSources.With("""
            [Command] public record RegisterAuthor([Key] Guid Id, Guid OtherId, string Name)
            {
            """ + handler + "}");
        Generate((Analyzed.SlicePath, source));

        Result.Source.ShouldNotContain(" identifier");
        Result.Source.ShouldNotContain("for id");
        Result.Model.Slices.SelectMany(slice => slice.Commands).Single().Produces.Single().UsesCommandContext.ShouldBeFalse();
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnrepresentableProductionDestination).ShouldBeTrue();
        AssertDocument();
    }
}
