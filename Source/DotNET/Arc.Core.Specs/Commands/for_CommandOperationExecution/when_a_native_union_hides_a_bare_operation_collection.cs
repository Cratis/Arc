// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET11_0_OR_GREATER
using Cratis.Arc.Commands.ModelBound;

namespace Cratis.Arc.Commands.for_CommandOperationExecution;

public class when_a_native_union_hides_a_bare_operation_collection : given.an_operation_pipeline
{
    async Task Because() => _result = await Run(new Command());

    [Fact] void should_fail() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_require_an_explicit_batch() => _result.ExceptionMessages.ShouldContain("Use CommandOperations instead of returning an ordinary collection of operation declarations.");
    [Fact] void should_not_execute_any_operations() => _log.Calls.ShouldBeEmpty();

    // Compiler-generated union struct; equality is not meaningful for a handler return.
#pragma warning disable CA1815
    public readonly union Outcome(List<given.NamedOperation>, string);
#pragma warning restore CA1815
    [Command] public record Command { public Outcome Handle() => new List<given.NamedOperation> { new("hidden") }; }
}
#endif
