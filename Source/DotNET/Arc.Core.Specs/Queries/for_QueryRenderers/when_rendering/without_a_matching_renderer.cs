// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryRenderers.when_rendering;

public class without_a_matching_renderer : given.query_renderers
{
    public record Query;

    object _query;
    QueryRendererResult _result;

    void Establish()
    {
        _query = new Query();
        Construct();
    }

    void Because() => _result = _renderers.Render("Test Query", _query, _serviceProvider);

    [Fact] void should_use_the_total_items_of_the_current_context() => _result.TotalItems.ShouldEqual(42);
    [Fact] void should_return_the_query_as_data() => _result.Data.ShouldEqual(_query);
}
