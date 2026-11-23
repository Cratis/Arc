// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET11_0_OR_GREATER
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Commands.for_CommandOperationExecution;

public class when_a_native_union_declares_an_operation : given.an_operation_pipeline
{
    async Task Because() => _result = await Run(new Command());

    [Fact] void should_execute_the_active_operation_once() => _log.Calls.ShouldEqual(["execute:active"]);
    [Fact] void should_not_expose_an_operation_response() => _result.ResponseValue.ShouldBeNull();
    [Fact] void should_report_the_started_operation() => _result.Recovery.StartedCount.ShouldEqual(1);
    [Fact] void should_complete_successfully() => _result.IsSuccess.ShouldBeTrue();

    // Compiler-generated union struct; equality is not meaningful for a handler return.
#pragma warning disable CA1815
    public readonly union Outcome(given.NamedOperation, ValidationResult);
#pragma warning restore CA1815
    [Command] public record Command { public Outcome Handle() => new given.NamedOperation("active"); }
}
#endif
