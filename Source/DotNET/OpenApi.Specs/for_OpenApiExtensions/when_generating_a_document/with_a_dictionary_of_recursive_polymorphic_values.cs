// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Arc.OpenApi.for_OpenApiExtensions.when_generating_a_document.given;

namespace Cratis.Arc.OpenApi.for_OpenApiExtensions.when_generating_a_document;

public class with_a_dictionary_of_recursive_polymorphic_values : Specification
{
    JsonNode? _document;

    async Task Because() => _document = await RecursiveSchemas.GenerateDictionary<RecursiveSchemas.INode>();

    [Fact] void should_reference_only_existing_components() => RecursiveSchemas.ReferencesResolve(_document!).ShouldBeTrue();
    [Fact] void should_reference_the_value_component() => _document!["components"]!["schemas"]!["DictionaryOfSomeConceptAndINode"]!["additionalProperties"]!["$ref"]!.ToString().ShouldEqual("#/components/schemas/INode");
    [Fact] void should_reference_the_parent_component() => _document!["components"]!["schemas"]!["INode"]!["properties"]!["parent"]!["oneOf"]![1]!["$ref"]!.ToString().ShouldEqual("#/components/schemas/INode");
    [Fact] void should_reference_the_children_component() => _document!["components"]!["schemas"]!["INode"]!["properties"]!["children"]!["items"]!["$ref"]!.ToString().ShouldEqual("#/components/schemas/INode");
}
