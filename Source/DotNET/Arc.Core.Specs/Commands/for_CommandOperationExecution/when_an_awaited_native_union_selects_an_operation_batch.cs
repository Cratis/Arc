// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET11_0_OR_GREATER
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Commands.for_CommandOperationExecution;

public class when_an_awaited_native_union_selects_an_operation_batch : given.an_operation_pipeline
{
    async Task Because() => _result = await Run(new Command());

    [Fact] void should_execute_in_declaration_order() => _log.Calls.ShouldEqual(["execute:first", "execute:second"]);
    [Fact] void should_return_the_tuple_response() => _result.ResponseValue.ShouldEqual("receipt");
    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();

    // Compiler-generated union struct; equality is not meaningful for a handler return.
#pragma warning disable CA1815
    public readonly union Outcome((string, CommandOperations), ValidationResult);
#pragma warning restore CA1815
    [Command] public record Command
    {
        public Task<Outcome> Handle() => Task.FromResult<Outcome>(("receipt", new CommandOperations([new given.NamedOperation("first"), new given.NamedOperation("second")])));
    }
}
#endif
