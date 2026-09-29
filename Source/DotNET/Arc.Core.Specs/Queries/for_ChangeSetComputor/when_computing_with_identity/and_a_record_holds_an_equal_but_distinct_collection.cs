// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ChangeSetComputor.when_computing_with_identity;

/// <summary>
/// A record overrides equality, so its own <see cref="object.Equals(object)"/> decides - even when it compares a
/// collection by reference and the serialized forms would have been identical.
/// </summary>
public class and_a_record_holds_an_equal_but_distinct_collection : given.a_change_set_computor
{
    static readonly Guid _id = new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1);

    ChangeSet _result;

    void Because() => _result = _computor.Compute(
        [new given.RecordWithIdAndTags(_id, ["a", "b"])],
        [new given.RecordWithIdAndTags(_id, ["a", "b"])]);

    [Fact] void should_report_the_item_as_replaced() => _result.Replaced.Cast<given.RecordWithIdAndTags>().Single().Id.ShouldEqual(_id);
    [Fact] void should_report_nothing_added() => _result.Added.ShouldBeEmpty();
    [Fact] void should_report_nothing_removed() => _result.Removed.ShouldBeEmpty();
}
