// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.ProxyGenerator.ModelBound.for_QueryExtensions.when_reading_the_http_method_attribute;

public class and_there_is_no_attribute : Specification
{
    IEnumerable<Templates.QueryDescriptor> _result;

    void Because() => _result = typeof(TestTypes.HttpMethods.NotDecorated).GetTypeInfo().ToQueryDescriptors(
        "/output",
        segmentsToSkip: 5,
        skipQueryNameInRoute: true,
        apiPrefix: "api",
        [typeof(TestTypes.HttpMethods.NotDecorated).GetTypeInfo()]);

    [Fact] void should_not_set_an_http_method() => _result.First().HttpMethod.ShouldBeNull();
}
