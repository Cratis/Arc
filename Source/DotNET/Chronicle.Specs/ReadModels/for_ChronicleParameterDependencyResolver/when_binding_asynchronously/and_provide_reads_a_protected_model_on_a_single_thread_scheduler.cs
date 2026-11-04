// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver.when_binding_asynchronously;

public class and_provide_reads_a_protected_model_on_a_single_thread_scheduler : given.a_gated_protected_read
{
    IReadOnlyList<object> _provided;
    Exception _failure;

    async Task Because()
    {
        var invoker = new CommandProvideInvoker();
        var run = _scheduler.Run(async () =>
        {
            using var policy = for_CommandDecisionReads.DecisionPolicyForSpecs.Begin(typeof(Transfer));
            CommandTransaction.Current = _unit;
            using var frame = _unit.EnforceReadProtection();
            CommandDecisionReads.Begin(typeof(Transfer));
            try
            {
                return (await invoker.Invoke(_context with { Type = typeof(Transfer), Command = new Transfer(new("from"), new("to")) }, _services)).ToArray();
            }
            finally
            {
                CommandDecisionReads.End();
            }
        });
        _failure = await Catch.Exception(async () =>
        {
            await ReleaseAfterEntry();
            _provided = await run.WaitAsync(_bound, TestContext.Current.CancellationToken);
        });
    }

    [Fact] void should_complete_without_blocking_the_scheduler() => _failure.ShouldBeNull();
    [Fact] void should_provide_the_released_model() => _provided[0].ShouldBeSame(_from);
}
