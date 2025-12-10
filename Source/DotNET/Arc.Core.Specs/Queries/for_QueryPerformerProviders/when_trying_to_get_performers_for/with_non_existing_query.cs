// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryPerformerProviders.when_trying_to_get_performers_for;

public class with_non_existing_query : given.an_initialized_query_performer_providers
{
    FullyQualifiedQueryName _queryName;
    bool _result;
    IQueryPerformer _performer;

    void Establish() => _queryName = new FullyQualifiedQueryName("NonExisting.Query");

    void Because() => _result = _queryPerformerProviders.TryGetPerformersFor(_queryName, out _performer);

    [Fact] void should_return_false() => _result.ShouldBeFalse();
    [Fact] void should_return_null_performer() => _performer.ShouldBeNull();
}