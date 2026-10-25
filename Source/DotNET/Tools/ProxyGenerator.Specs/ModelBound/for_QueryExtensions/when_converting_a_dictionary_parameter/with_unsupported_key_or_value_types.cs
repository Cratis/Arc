// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.ProxyGenerator.ModelBound.for_QueryExtensions.TestTypes.DictionaryParameters;

namespace Cratis.Arc.ProxyGenerator.ModelBound.for_QueryExtensions.when_converting_a_dictionary_parameter;

public class with_unsupported_key_or_value_types
{
    [Theory]
    [InlineData(nameof(DictionaryParameterReadModel.GetComplex))]
    [InlineData(nameof(DictionaryParameterReadModel.GetNumericKeys))]
    public void should_not_expose_the_dictionary_as_a_query_argument(string method)
    {
        var query = typeof(DictionaryParameterReadModel).GetTypeInfo().ToQueryDescriptors(
            "/output", 5, true, "api", [typeof(DictionaryParameterReadModel).GetTypeInfo()]).Single(query => query.Name == method);
        query.Parameters.ShouldBeEmpty();
        query.RequiredParameters.ShouldBeEmpty();
    }
}
