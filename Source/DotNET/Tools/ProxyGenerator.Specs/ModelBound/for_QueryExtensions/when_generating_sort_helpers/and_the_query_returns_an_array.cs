// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.ProxyGenerator.ModelBound.for_QueryExtensions.TestTypes.Sorting;
using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.ModelBound.for_QueryExtensions.when_generating_sort_helpers;

public class and_the_query_returns_an_array : Specification
{
    QueryDescriptor _descriptor;
    string _proxy;

    void Because()
    {
        var readModel = typeof(Listing).GetTypeInfo();
        _descriptor = readModel.ToQueryDescriptors("/output", 0, false, "api", [readModel]).Single(_ => _.Name == nameof(Listing.AllAsArray));
        _proxy = TemplateTypes.Query(_descriptor);
    }

    [Fact] void should_use_the_element_fields_for_sorting() => _descriptor.Properties.Select(_ => _.Name).ShouldContainOnly(nameof(Listing.Name), nameof(Listing.Price));
    [Fact] void should_generate_name_sorting_for_the_query() => _proxy.ShouldContain("new SortingActionsForQuery<Listing[]>('name', query)");
    [Fact] void should_generate_price_sorting_for_the_query() => _proxy.ShouldContain("new SortingActionsForQuery<Listing[]>('price', query)");
    [Fact] void should_not_generate_a_sort_helper_for_the_id_parameter() => _proxy.ShouldNotContain("get id()");
    [Fact] void should_preserve_the_id_parameter() => _descriptor.Parameters.Select(_ => _.Name).ShouldContainOnly("id");
}
