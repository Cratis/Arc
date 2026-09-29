// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;

namespace Cratis.Arc.Commands.for_CommandResultFactories.when_creating_without_a_factory;

public class and_reflection_is_not_supported : Specification
{
    record Response(string Value);

    readonly PlatformNotSupportedException _failure = new("no instantiation compiled");
    Exception _error;

    void Because() => _error = Catch.Exception(() => CommandResultFactories.CreateWithoutFactory(CorrelationId.New(), new Response("value"), (_, _) => throw _failure));

    [Fact] void should_throw_missing_command_result_factory() => _error.ShouldBeOfExactType<MissingCommandResultFactory>();
    [Fact] void should_keep_the_failure() => _error.InnerException.ShouldEqual(_failure);
}
