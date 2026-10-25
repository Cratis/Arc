// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.ProxyGenerator.ModelBound.for_QueryExtensions.TestTypes.DictionaryParameters;

namespace Cratis.Arc.ProxyGenerator.ModelBound.for_QueryExtensions.when_converting_a_dictionary_parameter;

public class with_string_values : Specification
{
    Templates.QueryDescriptor _result;

    void Because() => _result = typeof(DictionaryParameterReadModel).GetTypeInfo().ToQueryDescriptors(
        "/output", 5, true, "api", [typeof(DictionaryParameterReadModel).GetTypeInfo()])
        .Single(query => query.Name == nameof(DictionaryParameterReadModel.Get));

    [Fact] void should_emit_a_keyed_object() => _result.Parameters.Single().Type.ShouldEqual("Record<string, string>");
    [Fact] void should_use_the_object_constructor() => typeof(IDictionary<string, string>).GetTargetType().Constructor.ShouldEqual("Object");
    [Fact] void should_not_emit_an_array() => _result.Parameters.Single().IsEnumerable.ShouldBeFalse();
    [Fact] void should_require_the_dictionary() => _result.RequiredParameters.Select(parameter => parameter.Name).ShouldContain("parts");
    [Fact] void should_exclude_the_dependency() => _result.Parameters.Select(parameter => parameter.Name).ShouldNotContain("dependency");
}
