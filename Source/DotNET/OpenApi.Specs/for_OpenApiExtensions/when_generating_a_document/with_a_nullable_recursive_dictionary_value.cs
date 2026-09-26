// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Arc.OpenApi.for_OpenApiExtensions.when_generating_a_document.given;

namespace Cratis.Arc.OpenApi.for_OpenApiExtensions.when_generating_a_document;

public class with_a_nullable_recursive_dictionary_value : Specification
{
    JsonNode? _document;

    async Task Because() => _document = await RecursiveSchemas.GenerateDictionary<RecursiveSchemas.NullableNode>();

    [Fact] void should_allow_null_for_the_next_node() => _document!["components"]!["schemas"]!["NullableNode"]!["properties"]!["next"]!["oneOf"]![0]!["type"]!.ToString().ShouldEqual("null");
    [Fact] void should_reference_the_next_node() => _document!["components"]!["schemas"]!["NullableNode"]!["properties"]!["next"]!["oneOf"]![1]!["$ref"]!.ToString().ShouldEqual("#/components/schemas/NullableNode");
}
