// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.ProxyGenerator.ModelBound.for_QueryExtensions.TestTypes.EnumParameters;

namespace Cratis.Arc.ProxyGenerator.ModelBound.for_QueryExtensions.when_converting_an_enum_parameter;

public class and_it_is_a_plain_enum : Specification
{
    Templates.QueryDescriptor _result;
    Templates.RequestParameterDescriptor _parameter;

    void Because()
    {
        _result = typeof(EnumParameterReadModel).GetTypeInfo().ToQueryDescriptors(
            "/output",
            segmentsToSkip: 5,
            skipQueryNameInRoute: true,
            apiPrefix: "api",
            [typeof(EnumParameterReadModel).GetTypeInfo()]).Single(_ => _.Name == nameof(EnumParameterReadModel.GetByStatus));
        _parameter = _result.Parameters.Single();
    }

    [Fact] void should_include_the_parameter() => _parameter.Name.ShouldEqual("status");
    [Fact] void should_have_the_enum_name_as_type() => _parameter.Type.ShouldEqual(nameof(Status));
    [Fact] void should_have_number_as_constructor() => _parameter.Constructor.ShouldEqual("Number");
    [Fact] void should_be_required() => _result.RequiredParameters.Select(_ => _.Name).ShouldContain("status");
}
