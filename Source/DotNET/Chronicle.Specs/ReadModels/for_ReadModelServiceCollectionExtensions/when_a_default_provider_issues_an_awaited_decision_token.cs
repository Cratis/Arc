// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.DependencyInjection;
using Cratis.Chronicle;
using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadModelServiceCollectionExtensions;

public class when_a_default_provider_issues_an_awaited_decision_token : for_ChronicleParameterDependencyResolver.given.a_gated_protected_read
{
    ServiceProvider _defaultProvider;
    bool _completedBeforeRelease;
    object? _token;
    Exception _failure;

    void Establish()
    {
        var artifacts = Substitute.For<IClientArtifactsProvider>();
        artifacts.ModelBoundProjections.Returns([typeof(Balance)]);
        var accessor = Substitute.For<ICommandContextAccessor>();
        accessor.Current.Returns(_ => _context);
        _defaultProvider = new ServiceCollection()
            .AddSingleton(accessor)
            .AddReadModels(artifacts)
            .AddSingleton(_services.GetRequiredService<IDecisionReads>())
            .BuildServiceProvider();
    }

    async Task Because()
    {
        var parameter = typeof(Handler).GetMethod(nameof(Handler.Handle))!.GetParameters()[0];
        var run = _scheduler.Run(async () =>
        {
            using var policy = for_CommandDecisionReads.DecisionPolicyForSpecs.Begin(typeof(Transfer));
            CommandTransaction.Current = _unit;
            using var frame = _unit.EnforceReadProtection();
            CommandDecisionReads.Begin(typeof(Transfer));
            try
            {
                var pending = ParameterDependencyResolver.ResolveAsync(_defaultProvider, parameter, _ => new InvalidOperationException());
                _completedBeforeRelease = pending.IsCompleted;
                return await pending;
            }
            finally
            {
                CommandDecisionReads.End();
            }
        });
        _failure = await Catch.Exception(async () =>
        {
            await ReleaseAfterEntry();
            _token = await run.WaitAsync(_bound, TestContext.Current.CancellationToken);
        });
    }

    [Fact] void should_not_block_the_scheduler() => _failure.ShouldBeNull();
    [Fact] void should_yield_until_acquisition_finishes() => _completedBeforeRelease.ShouldBeFalse();
    [Fact] void should_issue_the_command_token() => ((DecisionRead<Balance>)_token!).IsProtected.ShouldBeTrue();

    void Destroy() => _defaultProvider.Dispose();

    class Handler
    {
        public void Handle(DecisionRead<Balance> balance) { }
    }
}
