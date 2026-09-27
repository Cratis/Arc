// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Arc.OpenApi.for_OpenApiExtensions.when_generating_a_document.given;

namespace Cratis.Arc.OpenApi.for_OpenApiExtensions.when_generating_a_document;

public class with_a_top_level_concept_array_response : Specification
{
    JsonNode? _document;

    async Task Because() => _document = await ConceptArrays.GenerateDocument();

    [Fact] void should_describe_the_response_as_an_array() => ResponseSchema()["type"]!.ToString().ShouldEqual("array");
    [Fact] void should_reference_the_primitive_string_items() => ResponseSchema()["items"]!["$ref"]!.ToString().ShouldEqual("#/components/schemas/Key");
    [Fact] void should_describe_the_referenced_items_as_strings() => _document!["components"]!["schemas"]!["Key"]!["type"]!.ToString().ShouldEqual("string");

    JsonNode ResponseSchema() => _document!["paths"]!["/keys"]!["get"]!["responses"]!["200"]!["content"]!["application/json"]!["schema"]!;
}
