// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Arc.Screenplay.Verification;
using Cratis.Screenplay;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.from_an_unproven_production_on_a_routed_command;

public class with_a_uuid_identifier : a_generated_document
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_withhold_the_whole_route(bool authoring)
    {
        const string Source = """
            using System;
            using Cratis.Arc.Commands.ModelBound;
            using Cratis.Arc.Chronicle.Commands;
            using Cratis.Chronicle.Events;
            using Cratis.Chronicle.EventSequences;
            using Cratis.Chronicle.Keys;

            namespace Library.Authors.Registration;

            [EventType]
            public record AuthorRegistered(string Name);

            [Command, EventSourceType("Account"), EventStreamType("Transactions")]
            public record RegisterAuthor([Key] Guid Id, Guid OtherId, string Name)
            {
                public EventForEventSourceId Handle() => new(OtherId, new AuthorRegistered(Name));
            }
            """;
        Generate(new ScreenplayOptions { AuthoringOnlyConstructs = authoring }, (Analyzed.SlicePath, Source));

        Result.Model.Slices.SelectMany(slice => slice.Commands).Single().Authoring!.Route!.IdentifierType!.Name.ShouldEqual("Uuid");
        Result.Source.ShouldNotContain("stream Account.Transactions");
        Result.Source.ShouldNotContain("eventsource Account");
        Result.Source.ShouldNotContain(" identifier");
        Result.Source.ShouldNotContain("for id");
        Result.Source.ShouldContain("produces AuthorRegistered");
        Result.Diagnostics.Count(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnrepresentableProductionDestination).ShouldEqual(1);
        Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandRoute).Message.ShouldContain("source and stream routing is not proven");
        Result.Diagnostics.Where(diagnostic => string.Equals(diagnostic.Code, ScreenplayDiagnosticCodes.SourceDidNotCompile, StringComparison.Ordinal) || string.Equals(diagnostic.Code, ScreenplayDiagnosticCodes.DocumentDidNotCompile, StringComparison.Ordinal) || string.Equals(diagnostic.Code, ScreenplayDiagnosticCodes.DocumentDidNotBind, StringComparison.Ordinal)).ShouldBeEmpty();
        var compiled = new ScreenplayCompiler().Compile(Result.Source);
        compiled.Diagnostics.Where(diagnostic => diagnostic.Severity is Cratis.Screenplay.Diagnostics.DiagnosticSeverity.Error or Cratis.Screenplay.Diagnostics.DiagnosticSeverity.Warning).ShouldBeEmpty();
        RoundTrip.Errors.ShouldBeEmpty();
        RoundTrip.IsStable.ShouldBeTrue();
        Bound.Diagnostics.Where(diagnostic => diagnostic.Severity == Cratis.Screenplay.Diagnostics.DiagnosticSeverity.Error && !ExpectedBindingDiagnostics.IsExpected(diagnostic, authoring)).ShouldBeEmpty();
    }
}
