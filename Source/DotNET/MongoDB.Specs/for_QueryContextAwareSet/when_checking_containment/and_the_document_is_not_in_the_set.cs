// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_QueryContextAwareSet.when_checking_containment;

public class and_the_document_is_not_in_the_set : Specification
{
    QueryContextAwareSet<SomeClassWithSomeId> _set;
    SomeId _absent;
    bool _result;

    void Establish()
    {
        _set = new(QueryContextBuilder.New().Build());
        _set.Add(new(Guid.NewGuid(), 42));
        _absent = Guid.NewGuid();
    }

    void Because() => _result = _set.Contains(_absent);

    [Fact] void should_not_find_it() => _result.ShouldBeFalse();
}
