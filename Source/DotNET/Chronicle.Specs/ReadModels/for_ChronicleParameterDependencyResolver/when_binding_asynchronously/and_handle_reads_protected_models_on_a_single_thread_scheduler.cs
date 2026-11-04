// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver.when_binding_asynchronously;

public class and_handle_reads_protected_models_on_a_single_thread_scheduler : given.a_gated_protected_read
{
    ICommandHandler _handler;
    CommandHandlerArgumentResolution _resolution;
    Exception _failure;

    void Establish()
    {
        _handler = Substitute.For<ICommandHandler>();
        _handler.Parameters.Returns(HandleParameters);
    }

    async Task Because()
    {
        var resolver = new CommandHandlerArgumentResolver(_provideInvoker);
        var run = _scheduler.Run(async () =>
        {
            using var policy = for_CommandDecisionReads.DecisionPolicyForSpecs.Begin(typeof(Transfer));
            CommandTransaction.Current = _unit;
            using var frame = _unit.EnforceReadProtection();
            CommandDecisionReads.Begin(typeof(Transfer));
            try
            {
                return await resolver.Resolve(_handler, _context, _services, null);
            }
            finally
            {
                CommandDecisionReads.End();
            }
        });
        _failure = await Catch.Exception(async () =>
        {
            await ReleaseAfterEntry();
            _resolution = await run.WaitAsync(_bound, TestContext.Current.CancellationToken);
        });
    }

    [Fact] void should_complete_without_blocking_the_scheduler() => _failure.ShouldBeNull();
    [Fact] void should_bind_the_first_model() => _resolution.Arguments[0].ShouldBeSame(_from);
    [Fact] void should_bind_the_second_model() => _resolution.Arguments[1].ShouldBeSame(_to);
    [Fact] void should_release_each_model_once() => _stored.Received(1).Release(_from);
}
