// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.ProxyGenerator.ModelBound.for_QueryExtensions.TestTypes.EnumParameters;

namespace Cratis.Arc.ProxyGenerator.ModelBound.for_QueryExtensions.when_converting_an_enum_parameter;

public class and_it_sits_next_to_an_injected_dependency : Specification
{
    Templates.QueryDescriptor _result;

    void Because() => _result = typeof(EnumParameterReadModel).GetTypeInfo().ToQueryDescriptors(
        "/output",
        segmentsToSkip: 5,
        skipQueryNameInRoute: true,
        apiPrefix: "api",
        [typeof(EnumParameterReadModel).GetTypeInfo()]).Single(_ => _.Name == nameof(EnumParameterReadModel.GetByStatusWithDependency));

    [Fact] void should_include_the_enum_argument() => _result.Parameters.Select(_ => _.Name).ShouldContain("status");
    [Fact] void should_not_include_the_dependency() => _result.Parameters.Select(_ => _.Name).ShouldNotContain("dependency");
}
