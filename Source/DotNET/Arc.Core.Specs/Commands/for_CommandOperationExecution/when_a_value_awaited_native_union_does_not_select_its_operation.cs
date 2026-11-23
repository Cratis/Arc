// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET11_0_OR_GREATER
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Commands.for_CommandOperationExecution;

public class when_a_value_awaited_native_union_does_not_select_its_operation : given.an_operation_pipeline
{
    async Task Because() => _result = await Run(new Command());

    [Fact] void should_not_start_the_unselected_operation() => _log.Calls.ShouldBeEmpty();
    [Fact] void should_return_the_active_response() => _result.ResponseValue.ShouldEqual("receipt");
    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();

    // Compiler-generated union struct; equality is not meaningful for a handler return.
#pragma warning disable CA1815
    public readonly union Outcome(given.NamedOperation, string, ValidationResult);
#pragma warning restore CA1815
    [Command] public record Command { public ValueTask<Outcome> Handle() => new(new Outcome("receipt")); }
}
#endif
