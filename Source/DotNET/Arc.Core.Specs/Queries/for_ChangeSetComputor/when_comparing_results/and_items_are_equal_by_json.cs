// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ChangeSetComputor.when_comparing_results;

/// <summary>
/// An item type that overrides no equality is compared by its JSON.
/// </summary>
public class and_items_are_equal_by_json : given.a_change_set_computor
{
    bool _result;

    void Because() => _result = _computor.AreEquivalentResults(
        new[] { new given.MutableItemWithId { Id = Guid.Empty, Name = "First", Value = 10 } },
        new[] { new given.MutableItemWithId { Id = Guid.Empty, Name = "First", Value = 10 } });

    [Fact] void should_be_equivalent() => _result.ShouldBeTrue();
}
