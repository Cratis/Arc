// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Arc.OpenApi.for_OpenApiExtensions.when_generating_a_document.given;

namespace Cratis.Arc.OpenApi.for_OpenApiExtensions.when_generating_a_document;

public class with_a_recursive_polymorphic_type : Specification
{
    JsonNode? _document;

    async Task Because() => _document = await RecursiveSchemas.Generate(true);

    [Fact] void should_reference_the_parent() => _document!["components"]!["schemas"]!["INode"]!["properties"]!["parent"]!["$ref"]!.ToString().ShouldEqual("#/components/schemas/INode");
    [Fact] void should_reference_the_child_items() => _document!["components"]!["schemas"]!["INode"]!["properties"]!["children"]!["items"]!["$ref"]!.ToString().ShouldEqual("#/components/schemas/INode");
}
