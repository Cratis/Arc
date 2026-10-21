// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Arc.Commands.for_ExplicitOffsetAttribute.when_deserializing;

public class and_a_nested_collection_value_is_invalid : given.date_inputs
{
    Exception _exception;

    void Because() => _exception = Catch.Exception(() => JsonSerializer.Deserialize<NestedInput>("""{"items":[{"from":"2026-10-16T12:30Z"},{"from":"2026-10-16T12:30"}]}""", Options));

    [Fact] void should_preserve_the_refusal() => _exception.ShouldBeOfExactType<ValueRequiresExplicitOffset>();
    [Fact] void should_attach_the_nested_path() => ((JsonException)_exception).Path.ShouldEqual("$.items[1].from");
    [Fact] void should_preserve_the_message() => _exception.Message.ShouldEqual("The value requires an explicit offset or Z.");
}
