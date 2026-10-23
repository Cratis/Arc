// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ChangeSetComputor.when_comparing_results;

public class and_collections_have_different_counts : given.a_change_set_computor
{
    bool _result;

    void Because() => _result = _computor.AreEquivalentResults(new[] { 1, 2 }, new[] { 1, 2, 3 });

    [Fact] void should_not_be_equivalent() => _result.ShouldBeFalse();
}
