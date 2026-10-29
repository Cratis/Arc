// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.ProxyGenerator.ModelBound.for_QueryExtensions.TestTypes.Sorting;
using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.ModelBound.for_QueryExtensions.when_generating_sort_helpers;

public class and_the_query_has_a_query_property : Specification
{
    string _proxy;

    void Because()
    {
        var readModel = typeof(QueryNamedListing).GetTypeInfo();
        var descriptor = readModel.ToQueryDescriptors("/output", 0, false, "api", [readModel]).Single(_ => _.Name == nameof(QueryNamedListing.AllAsArray));
        _proxy = TemplateTypes.Query(descriptor);
    }

    string SortByClass() => _proxy[_proxy.IndexOf("SortBy {", StringComparison.Ordinal).._proxy.IndexOf("SortByWithoutQuery {", StringComparison.Ordinal)];

    [Fact] void should_generate_a_plain_constructor_parameter_named_query() => _proxy.ShouldContain("constructor(query: ");
    [Fact] void should_not_generate_a_readonly_query_parameter_property() => _proxy.ShouldNotContain("readonly query:");
    [Fact] void should_generate_exactly_one_query_accessor() => SortByClass().Split("get query()").Length.ShouldEqual(2);
    [Fact] void should_initialize_the_sorting_action_for_the_query_property() => _proxy.ShouldContain("new SortingActionsForQuery<QueryNamedListing[]>('query', query)");
}
