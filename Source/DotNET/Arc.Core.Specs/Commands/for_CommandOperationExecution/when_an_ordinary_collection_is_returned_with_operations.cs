// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using OneOf;

namespace Cratis.Arc.Commands.for_CommandOperationExecution;

public class when_an_ordinary_collection_is_returned_with_operations : given.an_operation_pipeline
{
    async Task Because() => _result = await Run(new Composed());

    [Fact] void should_preserve_the_ordinary_collection_response() => ((CommandResult<string[]>)_result).Response.ShouldEqual(["one", "two"]);
    [Fact] void should_flatten_nested_tuples_and_active_union_branches() => _log.Calls.ShouldEqual(["execute:A", "execute:B"]);
    [Fact] void should_keep_the_command_successful() => _result.IsSuccess.ShouldBeTrue();

    public record Composed
    {
        public ((string[] Response, ICommandOperation Operation) First, OneOf<CommandOperations, int> Second) Handle() =>
            ((["one", "two"], new given.NamedOperation("A")), CommandOperations.Create([new given.NamedOperation("B")]));
    }
}
