// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Validation;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Arc.Chronicle.Commands.for_ReadChanges;

public class when_describing_a_conflict : Specification
{
    readonly DecisionConflict _conflict = new(typeof(BookStock), (ReadModelKey)"isbn-1");

    [Fact]
    void should_keep_the_message_and_state_when_chronicle_does_not_report_the_event()
    {
        var result = ReadChanges.For(_conflict, typeof(BorrowBook), enforcing: true, services: null);
        result.Message.ShouldEqual("BookStock 'isbn-1' changed after BorrowBook read it. Nothing from this command was written; submit it again.");
        result.Reason.ShouldEqual(ValidationResultReason.ConcurrencyViolation);
        result.ReasonDetail.ShouldEqual("ReadChanged");
        var state = JsonSerializer.SerializeToElement(result.State);
        state.EnumerateObject().Select(_ => _.Name).ShouldContainOnly("readModel", "key");
    }

    [Fact]
    void should_name_an_unregistered_event_type_by_its_identifier()
    {
        var result = ReadChanges.For(
            _conflict with { ConflictingEventTypeId = new("book-borrowed"), ConflictingEventSequenceNumber = 1042ul },
            typeof(BorrowBook),
            enforcing: true,
            services: null);
        result.Message.ShouldEqual("BookStock 'isbn-1' changed after BorrowBook read it (book-borrowed at 1042). Nothing from this command was written; submit it again.");
    }

    [Fact]
    void should_name_the_position_when_only_the_position_is_reported()
    {
        var result = ReadChanges.For(_conflict with { ConflictingEventSequenceNumber = 7ul }, typeof(BorrowBook), enforcing: true, services: null);
        result.Message.ShouldEqual("BookStock 'isbn-1' changed after BorrowBook read it (an event at 7). Nothing from this command was written; submit it again.");
        var state = JsonSerializer.SerializeToElement(result.State);
        state.GetProperty("conflictingEventSequenceNumber").GetUInt64().ShouldEqual(7ul);
        state.GetProperty("conflictingEventType").ValueKind.ShouldEqual(JsonValueKind.Null);
    }

    public record BookStock;

    public record BorrowBook;
}
