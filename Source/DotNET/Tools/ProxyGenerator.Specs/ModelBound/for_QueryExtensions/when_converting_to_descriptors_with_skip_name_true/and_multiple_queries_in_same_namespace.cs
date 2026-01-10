// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.ProxyGenerator.ModelBound.for_QueryExtensions.when_converting_to_descriptors_with_skip_name_true;

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
        skipQueryNameInRoute: true,
        apiPrefix: "api",
        _allQueries);

    [Fact] void should_return_two_descriptors() => _result.Count().ShouldEqual(2);
    [Fact] void should_include_get_by_id_query_name_in_route() => _result.First(_ => _.Name == "GetById").Route.ShouldEqual("/api/testtypes/orders/get-by-id");
    [Fact] void should_include_get_all_query_name_in_route() => _result.First(_ => _.Name == "GetAll").Route.ShouldEqual("/api/testtypes/orders/get-all");
}
