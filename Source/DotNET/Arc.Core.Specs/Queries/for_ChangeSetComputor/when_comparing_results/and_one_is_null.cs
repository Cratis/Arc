// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ChangeSetComputor.when_comparing_results;

public class and_one_is_null : given.a_change_set_computor
{
    bool _bothNull;
    bool _oneNull;

    void Because()
    {
        _bothNull = _computor.AreEquivalentResults(null, null);
        _oneNull = _computor.AreEquivalentResults(null, "a");
    }

    [Fact] void should_treat_both_null_as_equivalent() => _bothNull.ShouldBeTrue();
    [Fact] void should_treat_one_null_as_different() => _oneNull.ShouldBeFalse();
}
