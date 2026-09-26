// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Arc.OpenApi.for_OpenApiExtensions.when_generating_a_document.given;

namespace Cratis.Arc.OpenApi.for_OpenApiExtensions.when_generating_a_document;

public class with_a_dictionary_of_recursive_list_values : Specification
{
    JsonNode? _document;

    async Task Because() => _document = await RecursiveSchemas.GenerateDictionary<RecursiveSchemas.ListNodeValue>();

    [Fact] void should_reference_the_child_items() => _document!["components"]!["schemas"]!["ListNodeValue"]!["properties"]!["children"]!["items"]!["$ref"]!.ToString().ShouldEqual("#/components/schemas/ListNodeValue");
    [Fact] void should_resolve_all_references() => RecursiveSchemas.ReferencesResolve(_document!).ShouldBeTrue();
}
