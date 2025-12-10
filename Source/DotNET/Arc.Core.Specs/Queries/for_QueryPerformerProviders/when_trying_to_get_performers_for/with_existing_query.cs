// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryPerformerProviders.when_trying_to_get_performers_for;

public class with_existing_query : given.an_initialized_query_performer_providers
{
    FullyQualifiedQueryName _queryName;
    bool _result;
    IQueryPerformer _performer;

    void Establish() => _queryName = _firstPerformer.FullyQualifiedName;

    void Because() => _result = _queryPerformerProviders.TryGetPerformersFor(_queryName, out _performer);

    [Fact] void should_return_true() => _result.ShouldBeTrue();
    [Fact] void should_return_the_correct_performer() => _performer.ShouldEqual(_firstPerformer);
}