// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.DependencyInjection;
using Cratis.Chronicle;
using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadModelServiceCollectionExtensions.given;

/// <summary>
/// Observes who issues a <c>DecisionRead&lt;T&gt;</c> token: the framework's asynchronous binding, the framework's
/// blocking container factory, or an application registration. A read issued outside container resolution stays gated until
/// the binding has returned incomplete, which proves it did not block. A read inside container resolution is known to block
/// by design, so it is released at once. Every wait is bounded and the gate is always opened before the scheduler is disposed.
/// </summary>
public class a_decision_token_ownership_probe : Specification
{
    protected const string Framework = "framework asynchronous binding";
    protected const string ContainerFactory = "framework blocking container factory";
    protected const string Application = "application registration";

    static readonly TimeSpan _bound = TimeSpan.FromSeconds(5);
    protected IServiceCollection _services;
    protected ServiceDescriptor _override;
    protected List<bool> _readerCalls;
    protected int _applicationCalls;
    protected IClientArtifactsProvider _artifacts;
    protected IDecisionReads _reader;
    protected ICommandContextAccessor _accessor;
    SingleThreadScheduler _scheduler;
    TaskCompletionSource _gate;

    void Establish()
    {
        _scheduler = new();
        _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _readerCalls = [];
        _applicationCalls = 0;
        var token = for_CommandDecisionReads.DecisionFixtures.Protected("application", new ModelBoundReadModel("application"));
        _override = ServiceDescriptor.Transient<DecisionRead<ModelBoundReadModel>>(_ =>
        {
            _applicationCalls++;
            return token;
        });
        _accessor = Substitute.For<ICommandContextAccessor>();
        _accessor.Current.Returns(new CommandContext(Cratis.Execution.CorrelationId.New(), typeof(Handler), new Handler(), [], new()
        {
            [WellKnownCommandContextKeys.EventSourceId] = (Cratis.Chronicle.Events.EventSourceId)"framework"
        }));
        _reader = Substitute.For<IDecisionReads>();
        _reader.Get<ModelBoundReadModel>(Arg.Any<ReadModelKey>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            var insideContainer = new StackTrace().GetFrames().Any(frame =>
                frame.GetMethod()?.DeclaringType?.Namespace == "Microsoft.Extensions.DependencyInjection.ServiceLookup");
            _readerCalls.Add(insideContainer);
            if (insideContainer)
            {
                _gate.TrySetResult();
            }
            return Gated(_gate);
        });
        _artifacts = Substitute.For<IClientArtifactsProvider>();
        _artifacts.ModelBoundProjections.Returns([typeof(ModelBoundReadModel)]);
        _services = new ServiceCollection().AddSingleton(_accessor).AddSingleton(_reader).AddReadModels(_artifacts).AddSingleton(_reader);
    }

    void Destroy()
    {
        _gate.TrySetResult();
        _scheduler.Dispose();
    }

    static async Task<DecisionRead<ModelBoundReadModel>> Gated(TaskCompletionSource gate)
    {
        await gate.Task;
        return for_CommandDecisionReads.DecisionFixtures.Protected("framework", new ModelBoundReadModel("framework"));
    }

    /// <summary>
    /// Builds a root through the default container; its registrations stay mutable after the build.
    /// </summary>
    /// <returns>The built root.</returns>
    protected ServiceProvider BuildPlain() => _services.BuildServiceProvider();

    /// <summary>
    /// Builds a root through Arc's protected provider factory, which freezes a registration snapshot.
    /// </summary>
    /// <returns>The built root.</returns>
    protected ServiceProvider BuildProtected() => (ServiceProvider)_services.BuildProtectedDecisionServiceProvider();

    /// <summary>
    /// Binds a <c>DecisionRead&lt;T&gt;</c> parameter once and reports which owner issued the token.
    /// </summary>
    /// <param name="provider">The provider to bind through: a root or a child scope.</param>
    /// <returns>The owner that issued the token.</returns>
    protected async Task<string> Bind(IServiceProvider provider)
    {
        var reads = _readerCalls.Count;
        var applications = _applicationCalls;
        var parameter = typeof(Handler).GetMethod(nameof(Handler.Handle))!.GetParameters()[0];
        var observed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var completedBeforeRelease = true;
        _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);

        var run = _scheduler.Run(async () =>
        {
            ValueTask<object?> pending;
            try
            {
                pending = ParameterDependencyResolver.ResolveAsync(provider, parameter, _ => new InvalidOperationException());
                completedBeforeRelease = pending.IsCompleted;
            }
            finally
            {
                observed.TrySetResult();
            }
            return await pending;
        });
        try
        {
            await observed.Task.WaitAsync(_bound, TestContext.Current.CancellationToken);
            _gate.TrySetResult();
            await run.WaitAsync(_bound, TestContext.Current.CancellationToken);
        }
        finally
        {
            _gate.TrySetResult();
        }

        if (_applicationCalls > applications)
        {
            return Application;
        }
        _readerCalls.Count.ShouldEqual(reads + 1);
        if (_readerCalls[reads])
        {
            return ContainerFactory;
        }
        completedBeforeRelease.ShouldBeFalse();
        return Framework;
    }

    class Handler
    {
        public void Handle(DecisionRead<ModelBoundReadModel> model) { }
    }
}
