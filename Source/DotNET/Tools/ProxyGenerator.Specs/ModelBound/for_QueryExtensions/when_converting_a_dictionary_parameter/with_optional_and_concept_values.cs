// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.ProxyGenerator.ModelBound.for_QueryExtensions.TestTypes.DictionaryParameters;

namespace Cratis.Arc.ProxyGenerator.ModelBound.for_QueryExtensions.when_converting_a_dictionary_parameter;

public class with_optional_and_concept_values : Specification
{
    Templates.QueryDescriptor _optional;
    Templates.QueryDescriptor _concepts;

    void Because()
    {
        var queries = typeof(DictionaryParameterReadModel).GetTypeInfo().ToQueryDescriptors(
            "/output", 5, true, "api", [typeof(DictionaryParameterReadModel).GetTypeInfo()]).ToArray();
        _optional = queries.Single(query => query.Name == nameof(DictionaryParameterReadModel.GetOptional));
        _concepts = queries.Single(query => query.Name == nameof(DictionaryParameterReadModel.GetConcepts));
    }

    [Fact] void should_keep_the_optional_parameter() => _optional.Parameters.Single().IsOptional.ShouldBeTrue();
    [Fact] void should_not_require_the_optional_dictionary() => _optional.RequiredParameters.ShouldBeEmpty();
    [Fact] void should_map_concept_values_to_their_primitive_type() => _concepts.Parameters.Single().Type.ShouldEqual("Record<string, string>");
    [Fact] void should_not_make_concept_dictionaries_arrays() => _concepts.Parameters.Single().IsEnumerable.ShouldBeFalse();
}
