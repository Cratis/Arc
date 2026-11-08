// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Queries;

namespace Cratis.Arc.Mcp.for_McpQueryArguments;

public class when_mapping_query_arguments : Specification
{
    Paging _pageOnly;
    Paging _sizeOnly;
    Paging _both;
    Paging _none;
    Paging _hugePageWithoutSize;
    Exception? _object;
    Exception? _arrayForSingleValue;
    Exception? _arrayElementObject;
    QueryArguments _collection;
    QueryArguments _scalar;
    IQueryPerformer _performer;

    void Establish()
    {
        _performer = Substitute.For<IQueryPerformer>();
        _performer.SupportsPaging.Returns(true);
        _performer.Parameters.Returns(new QueryParameters([
            new QueryParameter("Prefix", typeof(string), true),
            new QueryParameter("Tags", typeof(string[]), true)
        ]));
    }

    void Because()
    {
        _none = Map("{}").Paging;
        _pageOnly = Map("""{"page":3}""").Paging;
        _hugePageWithoutSize = Map("""{"page":2147483647}""").Paging;
        _sizeOnly = Map("""{"page_size":5}""").Paging;
        _both = Map("""{"page":2,"page_size":5}""").Paging;
        _object = Catch.Exception(() => Map("""{"prefix":{"a":1}}"""));
        _arrayForSingleValue = Catch.Exception(() => Map("""{"prefix":["a"]}"""));
        _arrayElementObject = Catch.Exception(() => Map("""{"tags":[{"a":1}]}"""));
        _collection = Map("""{"tags":["a","b"]}""").Arguments;
        _scalar = Map("""{"prefix":"a"}""").Arguments;
    }

    [Fact] void should_not_page_without_arguments() => _none.IsPaged.ShouldBeFalse();
    [Fact] void should_not_page_with_only_a_page() => _pageOnly.IsPaged.ShouldBeFalse();
    [Fact] void should_not_page_with_only_an_extreme_page() => _hugePageWithoutSize.IsPaged.ShouldBeFalse();
    [Fact] void should_page_with_only_a_page_size() => (_sizeOnly.IsPaged, (int)_sizeOnly.Page, (int)_sizeOnly.Size).ShouldEqual((true, 0, 5));
    [Fact] void should_page_with_a_page_and_a_page_size() => (_both.IsPaged, (int)_both.Page, (int)_both.Size).ShouldEqual((true, 2, 5));
    [Fact] void should_refuse_an_object_value() => _object.ShouldBeOfExactType<McpArgumentsInvalid>();
    [Fact] void should_refuse_an_array_for_a_single_value() => _arrayForSingleValue.ShouldBeOfExactType<McpArgumentsInvalid>();
    [Fact] void should_refuse_an_object_inside_an_array() => _arrayElementObject.ShouldBeOfExactType<McpArgumentsInvalid>();
    [Fact] void should_map_a_collection_parameter() => ((string?[])_collection["Tags"]).ShouldContainOnly("a", "b");
    [Fact] void should_map_a_scalar_parameter() => _scalar["Prefix"].ShouldEqual("a");

    (QueryArguments Arguments, Paging Paging) Map(string arguments)
    {
        var tool = new McpToolDescriptor("a_tool", "A tool.", McpToolKind.Query, null, ["app:read"], McpToolAccess.Read, null, McpToolAnnotations.ForRead);
        using var document = JsonDocument.Parse(arguments);
        var values = document.RootElement.EnumerateObject().ToDictionary(_ => _.Name, _ => _.Value.Clone());
        var call = new McpOperationCall(tool, null!, values, Substitute.For<IServiceProvider>(), CancellationToken.None);
        return McpQueryArguments.From(call, _performer);
    }
}
