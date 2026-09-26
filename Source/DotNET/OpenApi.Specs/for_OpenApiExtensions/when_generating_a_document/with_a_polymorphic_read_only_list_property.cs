// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Arc.OpenApi.for_OpenApiExtensions.when_generating_a_document.given;

namespace Cratis.Arc.OpenApi.for_OpenApiExtensions.when_generating_a_document;

public class with_a_polymorphic_read_only_list_property : Specification
{
    JsonNode? _document;

    async Task Because() => _document = await RecursiveSchemas.GeneratePolymorphicCollection(true);

    [Fact] void should_reference_the_child_items() => _document!["components"]!["schemas"]!["IListNode"]!["properties"]!["children"]!["items"]!["$ref"]!.ToString().ShouldEqual("#/components/schemas/IListNode");
    [Fact] void should_resolve_all_references() => RecursiveSchemas.ReferencesResolve(_document!).ShouldBeTrue();
}
