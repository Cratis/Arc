// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_an_event_with_an_unmapped_member : a_batch_a_document
{
    void Because() => Generate((Analyzed.SlicePath, """
        using System;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Keys;
        namespace Library.Authors.Registration;
        [EventType("AuthorNamed")]
        public record AuthorRegistered(string Name)
        {
            public string Note { get; init; } = "Persisted default";
        }
        [Command]
        public record RegisterAuthor([Key] Guid Id, string Name)
        {
            public AuthorRegistered Handle() => new(Name);
        }
        """));

    [Fact] void should_keep_the_event_standalone() => Result.Source.ShouldNotContain("produces event AuthorRegistered");
    [Fact] void should_preserve_the_unmapped_payload_member() => Result.Source.ShouldContain("note String");
    [Fact] void should_preserve_the_pin() => Result.Source.ShouldContain("id \"AuthorNamed\"");
    [Fact] void should_not_invent_a_mapping() => Result.Source.ShouldNotContain("note =");
    [Fact] void should_read_back_without_errors() => RoundTrip.Errors.ShouldBeEmpty();
    [Fact] void should_round_trip_stably() => RoundTrip.IsStable.ShouldBeTrue();
}
