// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Arc.Commands.for_ExplicitOffsetAttribute.when_deserializing;

public class and_an_explicit_offset_is_missing : given.date_inputs
{
    Exception _exception;

    void Because() => _exception = Catch.Exception(() => JsonSerializer.Deserialize<OffsetInput>("""{"from":"2026-10-16T12:00:00"}""", Options));

    [Fact] void should_preserve_the_refusal_type() => _exception.ShouldBeOfExactType<ValueRequiresExplicitOffset>();
    [Fact] void should_attach_the_member_path() => ((JsonException)_exception).Path.ShouldEqual("$.from");
    [Fact] void should_preserve_the_refusal_message() => _exception.Message.ShouldEqual("The value requires an explicit offset or Z.");
}
