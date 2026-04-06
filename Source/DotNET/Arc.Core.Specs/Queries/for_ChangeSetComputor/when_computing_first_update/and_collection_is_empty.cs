// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ChangeSetComputor.when_computing_first_update;

public class and_collection_is_empty : given.a_change_set_computor
{
    ChangeSet _result;

    void Because() => _result = _computor.Compute(null, []);

    [Fact] void should_have_empty_added() => _result.Added.ShouldBeEmpty();
    [Fact] void should_have_empty_replaced() => _result.Replaced.ShouldBeEmpty();
    [Fact] void should_have_empty_removed() => _result.Removed.ShouldBeEmpty();
}
