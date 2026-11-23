// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Validation;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Arc.Chronicle.Commands.for_ReadChanges;

public class when_describing_a_conflict : Specification
{
    [Theory]
    [InlineData(true, false, false)]
    [InlineData(true, true, false)]
    [InlineData(true, false, true)]
    [InlineData(true, true, true)]
    [InlineData(false, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, true)]
    public void should_only_describe_the_changed_read(bool enforcing, bool hasEventType, bool hasSequenceNumber)
    {
        var conflict = new DecisionConflict(typeof(BookStock), (ReadModelKey)"server-selected-key")
        {
            ConflictingEventTypeId = hasEventType ? new("book-borrowed") : null,
            ConflictingEventSequenceNumber = hasSequenceNumber ? 1042ul : null
        };
        var result = ReadChanges.For(conflict, typeof(BorrowBook), enforcing, services: null);
        result.Message.ShouldEqual(enforcing
            ? "BookStock changed after BorrowBook read it. Nothing from this command was written; submit it again."
            : "BookStock changed after it was read. Read it again and resubmit.");
        result.Reason.ShouldEqual(ValidationResultReason.ConcurrencyViolation);
        result.ReasonDetail.ShouldEqual(enforcing ? "ReadChanged" : null);
        var state = JsonSerializer.SerializeToElement(result.State);
        state.EnumerateObject().Select(_ => _.Name).ShouldContainOnly("readModel");
        state.GetProperty("readModel").GetString().ShouldEqual("BookStock");
    }

    public record BookStock;

    public record BorrowBook;
}
