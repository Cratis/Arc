// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Arc.Queries;

namespace Cratis.Arc.for_ConverterExtensions.when_converting_a_collection;

public class with_json_elements : Specification
{
    JsonObject[] _objects;
    JsonArray[] _arrays;
    JsonNode[] _nodes;
    JsonDocument[] _documents;
    Exception? _malformed;
    Exception? _wrongShape;
    Exception? _nullNode;

    void Because()
    {
        _objects = (JsonObject[])new[] { "{\"value\":42}" }.ConvertTo(typeof(JsonObject[]))!;
        _arrays = (JsonArray[])new[] { "[42]" }.ConvertTo(typeof(JsonArray[]))!;
        _nodes = (JsonNode[])new[] { "42" }.ConvertTo(typeof(JsonNode[]))!;
        _documents = (JsonDocument[])new[] { "null" }.ConvertTo(typeof(JsonDocument[]))!;
        _malformed = Catch.Exception(() => new[] { "{" }.ConvertTo(typeof(JsonObject[])));
        _wrongShape = Catch.Exception(() => new[] { "[]" }.ConvertTo(typeof(JsonObject[])));
        _nullNode = Catch.Exception(() => new[] { "null" }.ConvertTo(typeof(JsonNode[])));
    }

    void Destroy()
    {
        foreach (var document in _documents)
        {
            document.Dispose();
        }
    }

    [Fact] void should_parse_an_object() => _objects.Single()["value"]!.GetValue<int>().ShouldEqual(42);
    [Fact] void should_parse_an_array() => _arrays.Single().Single()!.GetValue<int>().ShouldEqual(42);
    [Fact] void should_parse_a_scalar_node() => _nodes.Single().GetValue<int>().ShouldEqual(42);
    [Fact] void should_allow_a_document_containing_null() => _documents.Single().RootElement.ValueKind.ShouldEqual(JsonValueKind.Null);
    [Fact] void should_reject_malformed_json() => _malformed.ShouldBeOfExactType<InvalidCollectionQueryArgument>();
    [Fact] void should_reject_a_different_json_shape() => _wrongShape.ShouldBeOfExactType<InvalidCollectionQueryArgument>();
    [Fact] void should_reject_a_null_node() => _nullNode.ShouldBeOfExactType<InvalidCollectionQueryArgument>();
}
