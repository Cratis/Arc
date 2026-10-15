// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.from_an_unproven_production_on_a_routed_command;

public class with_a_uuid_identifier : a_production_route_document
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_recover_the_uuid_destination_on_the_effective_route(bool authoring)
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
        Result.Source.ShouldContain("stream Account.Transactions");
        Result.Source.ShouldContain("eventsource Account");
        Result.Source.ShouldContain(" identifier");
        Result.Source.ShouldContain("for otherId");
        Result.Source.ShouldNotContain("for id");
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnrepresentableProductionDestination).ShouldBeFalse();
        Result.Diagnostics.Where(diagnostic => string.Equals(diagnostic.Code, ScreenplayDiagnosticCodes.SourceDidNotCompile, StringComparison.Ordinal) || string.Equals(diagnostic.Code, ScreenplayDiagnosticCodes.DocumentDidNotCompile, StringComparison.Ordinal) || string.Equals(diagnostic.Code, ScreenplayDiagnosticCodes.DocumentDidNotBind, StringComparison.Ordinal)).ShouldBeEmpty();
        AssertRoutedDocument();
    }
}
