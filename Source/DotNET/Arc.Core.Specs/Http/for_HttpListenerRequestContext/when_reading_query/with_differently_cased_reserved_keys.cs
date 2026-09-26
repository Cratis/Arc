// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;

namespace Cratis.Arc.Http.for_HttpListenerRequestContext.when_reading_query;

public class with_differently_cased_reserved_keys : given.an_http_listener_request_context
{
    QueryRequest _result;

    async Task Because()
    {
        var contextTask = _listener.GetContextAsync();
        _ = _httpClient.GetAsync("/test?ID=abc&PAGE=1&PAGESIZE=2&sortBy=name&SORTDIRECTION=desc");
        _context = await contextTask;
        _requestContext = new HttpListenerRequestContext(_context, _serviceProvider);
        var performer = Substitute.For<IQueryPerformer>();
        performer.Parameters.Returns(new QueryParameters([new QueryParameter("id", typeof(string))]));
        _result = await new QueryStringQueryRequestReader().Read(_requestContext, performer);
    }

    [Fact] void should_sort_with_the_generated_proxy_key() => ((string)_result.Sorting.Field).ShouldEqual("Name");
    [Fact] void should_sort_with_uppercase_direction() => _result.Sorting.Direction.ShouldEqual(SortDirection.Descending);
    [Fact] void should_page_with_uppercase_keys() => ((int)_result.Paging.Page).ShouldEqual(1);
    [Fact] void should_read_uppercase_page_size() => ((int)_result.Paging.Size).ShouldEqual(2);
    [Fact] void should_preserve_argument_matching() => _result.Arguments["id"].ShouldEqual("abc");
    [Fact] void should_not_include_reserved_keys_as_arguments() => _result.Arguments.Count.ShouldEqual(1);
}
