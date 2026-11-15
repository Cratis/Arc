// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Commands.for_CommandResult.given;
using Cratis.Execution;

namespace Cratis.Arc.Commands.for_CommandResult;

public class when_serializing_a_missing_handler_result : Specification
{
    string _json;
    string _typedJson;

    void Because()
    {
        var options = new JsonSerializerOptions().ConfigureArcDefaults();
        var result = CommandResult.MissingHandler(CorrelationId.New(), typeof(string));
        _json = JsonSerializer.Serialize(result, new ArcJsonSerializerContext(options).CommandResult);
        var typedResult = new CommandResult<string>("response");
        typedResult.MergeWith(result);
        _typedJson = JsonSerializer.Serialize(typedResult, new CommandResultJsonContext(new JsonSerializerOptions().ConfigureArcDefaults()).CommandResultString);
    }

    [Fact] void should_write_the_flag_with_arc_generated_metadata() => JsonElement.Parse(_json).GetProperty("isMissingHandler").GetBoolean().ShouldBeTrue();
    [Fact] void should_write_the_flag_for_typed_generated_metadata() => JsonElement.Parse(_typedJson).GetProperty("isMissingHandler").GetBoolean().ShouldBeTrue();
    [Fact] void should_preserve_the_typed_response() => JsonElement.Parse(_typedJson).GetProperty("response").GetString().ShouldEqual("response");
}
