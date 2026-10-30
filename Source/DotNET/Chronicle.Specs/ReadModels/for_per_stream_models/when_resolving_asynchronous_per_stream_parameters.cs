// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.ReadModels.for_CommandDecisionReads;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Arc.Chronicle.ReadModels.for_per_stream_models;

public class when_resolving_asynchronous_per_stream_parameters : given.a_monthly_account
{
    readonly TaskCompletionSource<MonthlyWithdrawals> _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    MonthlyWithdrawals _instance;
    MonthlyWithdrawals _result;
    IReadModels _models;

    void Establish()
    {
        _models = Substitute.For<IReadModels>();
        _models.Release(Arg.Any<MonthlyWithdrawals>()).Returns(call =>
        {
            _instance = call.Arg<MonthlyWithdrawals>();
            SynchronizationContext.Current!.Post(_ => _release.TrySetResult(_instance), null);
            return _release.Task;
        });
    }

    async Task Because()
    {
        await Append("2026-10", 500);
        await for_CommandDecisionReads.given.SingleThreadCommandPump.Run(async () =>
        {
            using var policy = DecisionPolicyForSpecs.Begin(typeof(Withdraw));
            Begin(new Withdraw(_account, "2026-10", 100));
            try
            {
                var services = new InvocationServices(_services, new CommandDecisionReads(_store.GetDecisionReads(), _store, _models));
                var handler = new ModelBoundCommandHandler(typeof(Withdraw), typeof(Withdraw).GetMethod(nameof(Withdraw.Handle))!);
                var result = await new CommandHandlerArgumentResolver(new CommandProvideInvoker()).Resolve(handler, _context, services, null);
                _result = (MonthlyWithdrawals)result.Arguments[0]!;
            }
            finally
            {
                _scope.End(_context);
            }
        }, () => _release.TrySetResult(_instance));
    }

    [Fact] void should_bind_the_exact_stream_instance_after_release() => _result.ShouldBeSame(_instance);
    [Fact] void should_read_only_the_command_month() => _result.Total.ShouldEqual(500);
    [Fact] void should_enroll_the_stream_read_before_exposure() => _unit.HasEnrolledDecisionReads.ShouldBeTrue();
    [Fact] void should_release_once() => _models.Received(1).Release(Arg.Any<MonthlyWithdrawals>());

    sealed class InvocationServices(IServiceProvider inner, IDecisionReads reads) : IServiceProvider
    {
        public object? GetService(Type serviceType) => serviceType == typeof(IDecisionReads) ? reads : inner.GetService(serviceType);
    }
}
