// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;

namespace Cratis.Arc.Commands.for_CommandResultFactories.when_creating_without_a_factory;

public class and_reflection_is_supported : Specification
{
    record Response(string Value);

    CommandResult _result;

    void Because() => _result = CommandResultFactories.CreateWithoutFactory(CorrelationId.New(), new Response("value"), CommandResultFactories.CreateThroughReflection);

    [Fact] void should_wrap_the_response_in_its_exact_result_type() => _result.ShouldBeOfExactType<CommandResult<Response>>();
    [Fact] void should_hold_the_response() => ((CommandResult<Response>)_result).Response.ShouldEqual(new Response("value"));
}
