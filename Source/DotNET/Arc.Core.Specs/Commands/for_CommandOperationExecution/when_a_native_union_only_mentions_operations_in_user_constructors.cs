// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET11_0_OR_GREATER
using Cratis.Arc.Commands.ModelBound;

namespace Cratis.Arc.Commands.for_CommandOperationExecution;

public class when_a_native_union_only_mentions_operations_in_user_constructors : given.an_operation_pipeline
{
    CommandResult _childResult;

    async Task Because() => _result = await Run(new Parent(async () => _childResult = await Run(new Child(_log))));

    [Fact] void should_allow_the_nested_child_handler() => _log.Calls.ShouldEqual(["child"]);
    [Fact] void should_return_the_actual_case() => _childResult.ResponseValue.ShouldEqual("receipt");
    [Fact] void should_not_classify_it_as_an_operation_collection() => CommandOperationBoundary.IsBareCollection(typeof(List<Outcome>)).ShouldBeFalse();

    // Compiler-generated union struct; equality is not meaningful for a handler return.
#pragma warning disable CA1815
    public readonly union Outcome(string, int)
    {
        public Outcome(given.NamedOperation operation, bool flag) : this("receipt") { }
        internal Outcome(CommandOperations operations) : this("receipt") { }
    }
#pragma warning restore CA1815
    [Command] public record Child(given.OperationLog Log)
    {
        public Outcome Handle()
        {
            Log.Calls.Add("child");
            return new Outcome(new given.NamedOperation("must-not-run"), true);
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
