// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.ProxyGenerator.ModelBound.for_QueryExtensions.TestTypes.EnumParameters;

namespace Cratis.Arc.ProxyGenerator.ModelBound.for_QueryExtensions.when_converting_an_enum_parameter;

/// <summary>
/// Pins the fix for a nullable enum query parameter: left wrapped, <c>Nullable&lt;Status&gt;</c> — not
/// <see cref="Status"/> — used to end up in <see cref="Templates.QueryDescriptor.TypesInvolved"/>, and the
/// generator would emit a bogus class reflecting <c>Nullable&lt;T&gt;</c>'s own <c>HasValue</c>/<c>Value</c>
/// properties under the enum's name instead of recognizing it as the enum.
/// </summary>
public class and_it_is_a_nullable_enum : Specification
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
            [typeof(EnumParameterReadModel).GetTypeInfo()]).Single(_ => _.Name == nameof(EnumParameterReadModel.GetByOptionalStatus));
        _parameter = _result.Parameters.Single();
    }

    [Fact] void should_include_the_parameter() => _parameter.Name.ShouldEqual("status");
    [Fact] void should_unwrap_the_original_type_to_the_enum() => _parameter.OriginalType.ShouldEqual(typeof(Status));
    [Fact] void should_have_the_enum_name_as_type() => _parameter.Type.ShouldEqual(nameof(Status));
    [Fact] void should_have_number_as_constructor() => _parameter.Constructor.ShouldEqual("Number");
    [Fact] void should_be_optional() => _result.RequiredParameters.Select(_ => _.Name).ShouldNotContain("status");
    [Fact] void should_not_collect_the_nullable_wrapper_as_a_type_involved() => _result.TypesInvolved.ShouldNotContain(typeof(Status?));
}
