// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.AspNetCore.Http;
using Cratis.Arc.Queries;
using Microsoft.AspNetCore.Http;

namespace Cratis.Arc.Http.for_AspNetCoreHttpRequestContext.when_reading_query;

public class with_differently_cased_reserved_keys : Specification
{
    QueryRequest _result;

    async Task Because()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.QueryString = new QueryString("?ID=abc&PAGE=1&PAGESIZE=2&sortBy=name&SORTDIRECTION=desc");
        var context = new AspNetCoreHttpRequestContext(httpContext);
        var performer = Substitute.For<IQueryPerformer>();
        performer.Parameters.Returns(new QueryParameters([new QueryParameter("id", typeof(string))]));
        _result = await new QueryStringQueryRequestReader().Read(context, performer);
    }

    [Fact] void should_sort_with_the_generated_proxy_key() => ((string)_result.Sorting.Field).ShouldEqual("Name");
    [Fact] void should_sort_with_uppercase_direction() => _result.Sorting.Direction.ShouldEqual(SortDirection.Descending);
    [Fact] void should_page_with_uppercase_keys() => ((int)_result.Paging.Page).ShouldEqual(1);
    [Fact] void should_read_uppercase_page_size() => ((int)_result.Paging.Size).ShouldEqual(2);
    [Fact] void should_preserve_argument_matching() => _result.Arguments["id"].ShouldEqual("abc");
    [Fact] void should_not_include_reserved_keys_as_arguments() => _result.Arguments.Count.ShouldEqual(1);
}
