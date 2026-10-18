// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_stating_event_collection_predicates;

public class with_an_explicit_comparer : given.an_event_collection_predicate
{
    void Because() => GenerateExpectation("e.Tags.SequenceEqual<ApprovalLabel>([Urgent, \"review\"], EqualityComparer<ApprovalLabel>.Default)");

    [Fact] void should_leave_comparer_overloads_unread() => AssertUnread();
}
