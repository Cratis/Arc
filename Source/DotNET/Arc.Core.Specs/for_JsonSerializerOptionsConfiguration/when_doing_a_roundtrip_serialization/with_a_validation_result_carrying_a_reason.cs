// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Validation;

namespace Cratis.Arc.for_JsonSerializerOptionsConfiguration.when_doing_a_roundtrip_serialization;

/// <summary>
/// The reason is only worth carrying if it reaches the client that has to branch on it. It is a concept, so it must
/// arrive as a plain string rather than as an object wrapping a value - that is the shape the TypeScript side reads.
/// </summary>
public class with_a_validation_result_carrying_a_reason : Specification
{
    JsonSerializerOptions _options;
    string _json;
    ValidationResult _deserialized;

    void Establish() => _options = new JsonSerializerOptions().ConfigureArcDefaults();

    void Because()
    {
        _json = JsonSerializer.Serialize(
            ValidationResult.Error("Something raced", reason: ValidationResultReason.ConcurrencyViolation),
            _options);
        _deserialized = JsonSerializer.Deserialize<ValidationResult>(_json, _options)!;
    }

    [Fact] void should_write_the_reason_as_a_plain_string() => _json.ShouldContain(@"""reason"":""concurrencyViolation""");
    [Fact] void should_round_trip_the_reason() => _deserialized.Reason.ShouldEqual(ValidationResultReason.ConcurrencyViolation);
    [Fact] void should_round_trip_the_message() => _deserialized.Message.ShouldEqual("Something raced");
}
