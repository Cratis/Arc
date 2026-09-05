// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.ProxyGenerator.ModelBound.for_QueryExtensions.when_reading_the_http_method_attribute;

public class and_it_is_on_the_method : Specification
{
    IEnumerable<Templates.QueryDescriptor> _result;

    void Because() => _result = typeof(TestTypes.HttpMethods.MethodDecorated).GetTypeInfo().ToQueryDescriptors(
        "/output",
        segmentsToSkip: 5,
        skipQueryNameInRoute: true,
        apiPrefix: "api",
        [typeof(TestTypes.HttpMethods.MethodDecorated).GetTypeInfo()]);

    [Fact] void should_set_the_http_method_from_the_method_attribute() => _result.First().HttpMethod.ShouldEqual("Query");
}
