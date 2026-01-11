// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.ProxyGenerator.ModelBound.for_QueryExtensions.when_converting_to_descriptors_with_skip_name_false;

public class and_multiple_queries_in_same_namespace : Specification
{
    IEnumerable<Templates.QueryDescriptor> _result;
    TypeInfo[] _allQueries;

    void Establish()
    {
        _allQueries = [typeof(TestTypes.Orders.Order).GetTypeInfo()];
    }

    void Because() => _result = typeof(TestTypes.Orders.Order).GetTypeInfo().ToQueryDescriptors(
        "/output",
        segmentsToSkip: 5,
        skipQueryNameInRoute: false,
        apiPrefix: "api",
        _allQueries);

    [Fact] void should_include_query_names_in_routes_regardless_of_conflict() => _result.All(_ => _.Route.Contains("get-")).ShouldBeTrue();
}
