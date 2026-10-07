// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_authoring_only_constructs;

public class a_generated_response_with_an_aggregate : an_authoring_document
{
    void Because() => Generate("""
        using System;
        using System.Threading.Tasks;
        using Cratis.Arc.Chronicle.Aggregates;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;
        namespace Library.Authors.Registration;
        public record AuthorId(Guid Value) : EventSourceId<Guid>(Value);
        [EventType] public record AuthorRegistered(string Name);
        [EventType] public record ReceiptIssued;
        public class Author : AggregateRoot
        {
            public async Task<int> Register(string name)
            {
                await Apply(new AuthorRegistered(name));
                return 0;
            }
            public void OnRegistered(AuthorRegistered @event) { }
        }
        [Command] public record RegisterAuthor(string Name)
        {
            public async Task<(AuthorId, ReceiptIssued)> Handle(Author author)
            {
                AuthorId authorId = new(Guid.NewGuid());
                var r = await author.Register(Name);
                return (authorId, new ReceiptIssued());
            }
        }
        """);

    [Fact] void should_bind_both_modes_as_v7() => AssertExecutableDocument();
    [Fact] void should_keep_the_generated_response() => Result.Source.ShouldContain("returns authorId");
    [Fact] void should_not_mark_the_response_as_the_aggregate_identifier() => Result.Model.Slices.SelectMany(slice => slice.Commands).Single().Authoring!.Identifier.ShouldBeNull();
    [Fact] void should_not_route_aggregate_events_to_the_generated_response() => Result.Source.Contains("for authorId", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_explain_the_unproven_destination_without_suggesting_the_enabled_option() => Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableEventSourceIdResult).Message.ShouldContain("could not be proven");
}
