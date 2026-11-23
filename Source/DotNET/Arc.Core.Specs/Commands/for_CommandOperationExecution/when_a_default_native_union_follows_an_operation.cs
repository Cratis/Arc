// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET11_0_OR_GREATER
using Cratis.Arc.Commands.ModelBound;

namespace Cratis.Arc.Commands.for_CommandOperationExecution;

public class when_a_default_native_union_follows_an_operation : given.an_operation_pipeline
{
    async Task Because() => _result = await Run(new Command());

    [Fact] void should_not_start_the_earlier_operation() => _log.Calls.ShouldBeEmpty();
    [Fact] void should_reject_the_invalid_graph() => _result.ExceptionMessages.ShouldContain("A command union must contain an active non-null value.");
    [Fact] void should_fail() => _result.IsSuccess.ShouldBeFalse();

    // Compiler-generated union struct; equality is not meaningful for a handler return.
#pragma warning disable CA1815
    public readonly union Outcome(string, int);
#pragma warning restore CA1815
    [Command] public record Command
    {
        public (given.NamedOperation, Outcome) Handle() => (new given.NamedOperation("must-not-run"), default);
    }
}
#endif
