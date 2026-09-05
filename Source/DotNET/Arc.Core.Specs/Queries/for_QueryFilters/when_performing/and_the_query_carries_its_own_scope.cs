// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryFilters.when_performing;

/// <summary>
/// A filter is resolved from the scope the query runs in, not from the provider that constructed this singleton,
/// so a filter depending on a scoped service is created in the scope rather than in the root — the same lifetime
/// seam the command pipeline has.
/// </summary>
public class and_the_query_carries_its_own_scope : given.a_query_filters
{
    IQueryFilter _filterFromTheScope;
    IQueryFilter _filterFromTheRoot;
    QueryContext _context;

    void Establish()
    {
        _filterFromTheScope = Substitute.For<IQueryFilter>();
        _filterFromTheRoot = Substitute.For<IQueryFilter>();

        var scope = Substitute.For<IServiceProvider>();
        scope.GetService(typeof(IInstancesOf<IQueryFilter>))
            .Returns(new KnownInstancesOf<IQueryFilter>([_filterFromTheScope]));

        _context = new("Test Query", _correlationId, Paging.NotPaged, Sorting.None, ServiceProvider: scope);
        _queryFilters = new(new KnownInstancesOf<IQueryFilter>([_filterFromTheRoot]), _activitySource);
    }

    async Task Because() => await _queryFilters.OnPerform(_context);

    [Fact] void should_run_the_filter_from_the_scope() => _filterFromTheScope.Received(1).OnPerform(_context);
    [Fact] void should_not_run_the_filter_from_the_root() => _filterFromTheRoot.DidNotReceive().OnPerform(Arg.Any<QueryContext>());
}
