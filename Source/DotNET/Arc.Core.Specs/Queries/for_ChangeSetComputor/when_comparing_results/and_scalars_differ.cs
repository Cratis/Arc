// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ChangeSetComputor.when_comparing_results;

public class and_scalars_differ : given.a_change_set_computor
{
    bool _result;

    void Because() => _result = _computor.AreEquivalentResults("a", "b");

    [Fact] void should_not_be_equivalent() => _result.ShouldBeFalse();
}
