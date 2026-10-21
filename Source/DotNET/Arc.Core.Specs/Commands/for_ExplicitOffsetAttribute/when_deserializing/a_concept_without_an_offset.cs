// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Arc.Commands.for_ExplicitOffsetAttribute.when_deserializing;

public class a_concept_without_an_offset : given.date_inputs
{
    Exception _exception;

    void Because() => _exception = Catch.Exception(() => JsonSerializer.Deserialize<ConceptOffsetInput>("""{"from":"2026-10-16T12:30"}""", Options));

    [Fact] void should_preserve_the_refusal_type() => _exception.ShouldBeOfExactType<ValueRequiresExplicitOffset>();
    [Fact] void should_attach_the_member_path() => ((JsonException)_exception).Path.ShouldEqual("$.from");
}
