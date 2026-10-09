// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Arc.Screenplay.Verification;
using Cratis.Screenplay;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_an_explicitly_routed_production_with_a_command_stream : a_generated_document
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_omit_the_unrepresentable_route_without_a_compiler_error(bool authoring)
    {
        const string Source = """
            using Cratis.Arc.Commands.ModelBound;
            using Cratis.Arc.Chronicle.Commands;
            using Cratis.Chronicle.Events;
            using Cratis.Chronicle.EventSequences;
            using Cratis.Chronicle.Keys;

            namespace Library.Authors.Registration;

            [EventType]
            public record AuthorRegistered(string Name);

            [Command, EventSourceType("Account"), EventStreamType("Transactions")]
            public record RegisterAuthor([Key] string Id, string OtherId, string Name)
            {
                public EventForEventSourceId Handle() => new(OtherId, new AuthorRegistered(Name));
            }
            """;
        Generate(new ScreenplayOptions { AuthoringOnlyConstructs = authoring }, (Analyzed.SlicePath, Source));

        Result.Source.ShouldNotContain("stream Account.Transactions");
        Result.Source.ShouldNotContain("eventsource Account");
        Result.Source.ShouldNotContain(" identifier");
        Result.Source.ShouldNotContain("for id");
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnrepresentableProductionDestination).ShouldBeTrue();
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandRoute).ShouldEqual(authoring);
        Result.Diagnostics.Where(diagnostic => string.Equals(diagnostic.Code, ScreenplayDiagnosticCodes.SourceDidNotCompile, StringComparison.Ordinal) || string.Equals(diagnostic.Code, ScreenplayDiagnosticCodes.DocumentDidNotCompile, StringComparison.Ordinal) || string.Equals(diagnostic.Code, ScreenplayDiagnosticCodes.DocumentDidNotBind, StringComparison.Ordinal)).ShouldBeEmpty();
        var compiled = new ScreenplayCompiler().Compile(Result.Source);
        compiled.Diagnostics.Where(diagnostic => diagnostic.Severity is Cratis.Screenplay.Diagnostics.DiagnosticSeverity.Error or Cratis.Screenplay.Diagnostics.DiagnosticSeverity.Warning).ShouldBeEmpty();
        RoundTrip.Errors.ShouldBeEmpty();
        RoundTrip.IsStable.ShouldBeTrue();
        Bound.Diagnostics.Where(diagnostic => diagnostic.Severity == Cratis.Screenplay.Diagnostics.DiagnosticSeverity.Error && !ExpectedBindingDiagnostics.IsExpected(diagnostic, authoring)).ShouldBeEmpty();
    }
}
