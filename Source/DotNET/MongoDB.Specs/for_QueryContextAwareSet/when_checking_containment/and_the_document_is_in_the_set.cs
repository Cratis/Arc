// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_QueryContextAwareSet.when_checking_containment;

public class and_the_document_is_in_the_set : Specification
{
    QueryContextAwareSet<SomeClassWithSomeId> _set;
    SomeClassWithSomeId _item;
    bool _result;

    void Establish()
    {
        _set = new(QueryContextBuilder.New().Build());
        _item = new(Guid.NewGuid(), 42);
        _set.Add(_item);
    }

    void Because() => _result = _set.Contains(_item.Id);

    [Fact] void should_find_it() => _result.ShouldBeTrue();
}
