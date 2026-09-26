// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Arc.OpenApi.for_OpenApiExtensions.when_generating_a_document.given;

namespace Cratis.Arc.OpenApi.for_OpenApiExtensions.when_generating_a_document;

public class with_a_recursive_dictionary_value : Specification
{
    JsonNode? _document;

    async Task Because() => _document = await RecursiveSchemas.Generate(false);

    [Fact] void should_reference_the_dictionary_value() => _document!["components"]!["schemas"]!["DictionaryOfSomeConceptAndNode"]!["additionalProperties"]!["$ref"]!.ToString().ShouldEqual("#/components/schemas/Node");
}
