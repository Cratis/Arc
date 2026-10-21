// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions;

public class from_date_ordering : a_generated_document
{
    [Fact]
    void should_keep_the_guard_in_code_and_name_the_refused_property()
    {
        Generate((Analyzed.SlicePath, """
            using System;
            using System.Collections.Generic;
            using Cratis.Chronicle.Events;
            using Cratis.Chronicle.Reactors;
            namespace Library.Lending.Reserving;
            [EventType] public record BookReserved(DateOnly Start, DateOnly End);
            [EventType] public record ReservationAccepted(DateOnly Start);
            public class ReservationAcceptor : IReactor
            {
                public IEnumerable<object> Accept(BookReserved @event) => @event.End >= @event.Start ? [new ReservationAccepted(@event.Start)] : [];
            }
            """));
        Result.Source.ShouldContain("reaction ReservationAcceptor");
        Result.Source.ShouldNotContain("where");
        Result.Source.ShouldNotContain("produces ReservationAccepted");
        Result.Diagnostics.Any(diagnostic => diagnostic.Message.Contains("@event.End", StringComparison.Ordinal) && diagnostic.Message.Contains("reaction where", StringComparison.Ordinal)).ShouldBeTrue();
        AssertDocument();
    }
}
