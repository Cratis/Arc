// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.ControllerBased.for_QueryPerformerProvider;

public class when_trying_to_get_controller_query_performer_for : given.a_controller_query_performer_provider
{
    bool _found;
    IQueryPerformer? _performer;

    void Because() => _found = _provider.TryGetPerformerFor(QueryName, out _performer);

    [Fact] void should_find_performer() => _found.ShouldBeTrue();
    [Fact] void should_return_performer() => _performer.ShouldNotBeNull();
    [Fact] void should_have_expected_fully_qualified_name() => _performer!.FullyQualifiedName.Value.ShouldEqual(QueryName);
}