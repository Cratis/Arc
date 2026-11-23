// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET11_0_OR_GREATER
using Cratis.Arc.Commands.ModelBound;

namespace Cratis.Arc.Commands.for_CommandOperationExecution;

public class when_a_nested_command_declares_operations_through_a_native_union : given.an_operation_pipeline
{
    CommandResult _childResult;

    async Task Because() => _result = await Run(new Parent(async () => _childResult = await Run(new Child(_log))));

    [Fact] void should_reject_before_executing_the_child_handler() => _log.Calls.ShouldBeEmpty();
    [Fact] void should_report_the_unsupported_boundary() => _childResult.ExceptionMessages.ShouldContain("Nested commands that may participate in command operations are unsupported. The child has not executed.");

    // Compiler-generated union struct; equality is not meaningful for a handler return.
#pragma warning disable CA1815
    public readonly union Outcome(given.NamedOperation, string);
#pragma warning restore CA1815
    [Command] public record Child(given.OperationLog Log)
    {
        public Outcome Handle()
        {
            Log.Calls.Add("child");
            return "receipt";
        }
    }
    [Command] public record Parent(Func<Task> ExecuteChild)
    {
        public async Task<string> Handle()
        {
            await ExecuteChild();
            return "parent";
        }
    }
}
#endif
