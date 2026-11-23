// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET11_0_OR_GREATER
using System.Text.Json;

namespace Cratis.Arc.Commands.for_CommandPipeline;

public class when_executing_a_native_union_response : given.a_native_union_pipeline
{
    async Task Because() => await Run(new Direct(_receipt));

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_return_the_active_case() => _result.ResponseValue.ShouldEqual(_receipt);
    [Fact] void should_not_invoke_server_handlers() => _commandResponseValueHandlers.DidNotReceive().Handle(Arg.Any<CommandContext>(), Arg.Any<object>());
    [Fact] void should_serialize_only_the_active_case() => JsonSerializer.Serialize<Answer>(_receipt, JsonSerializerOptions.Web).ShouldEqual("{\"number\":\"42\"}");
    [Fact] void should_serialize_the_response_in_the_envelope() => JsonSerializer.Serialize(_result, _result.GetType(), JsonSerializerOptions.Web).ShouldContain("\"response\":{\"number\":\"42\"}");
}
#endif
