// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.ProxyGenerator.ModelBound.for_QueryExtensions.when_converting_to_descriptors_with_skip_name_true;

public class and_single_query_in_namespace : Specification
{
    IEnumerable<Templates.QueryDescriptor> _result;
    TypeInfo[] _allQueries;

    void Establish()
    {
        _allQueries = [typeof(TestTypes.Products.Product).GetTypeInfo()];
    }

    void Because() => _result = typeof(TestTypes.Products.Product).GetTypeInfo().ToQueryDescriptors(
        "/output",
        segmentsToSkip: 5,
        skipQueryNameInRoute: true,
        apiPrefix: "api",
        _allQueries);

    [Fact] void should_return_one_descriptor() => _result.Count().ShouldEqual(1);
    [Fact] void should_not_include_query_name_in_route() => _result.First().Route.ShouldEqual("/api/test-types/products");
}
