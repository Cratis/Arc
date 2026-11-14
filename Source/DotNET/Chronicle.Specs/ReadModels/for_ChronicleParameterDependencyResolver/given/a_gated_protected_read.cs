// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver.given;

/// <summary>
/// A protected read whose acquisition completes only when the test opens the gate, with a fake client
/// that awaits the gate while capturing the caller's synchronization context.
/// </summary>
public class a_gated_protected_read : protected_plain_models
{
    protected static readonly TimeSpan _bound = TimeSpan.FromSeconds(5);
    protected SingleThreadScheduler _scheduler;
    protected TaskCompletionSource _entered;
    protected TaskCompletionSource _gate;
    protected TaskCompletionSource _observed;
    protected bool _openOnEntry;
    protected ICommandProvideInvoker _provideInvoker;

    void Establish()
    {
        _scheduler = new();
        _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _observed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _provideInvoker = Substitute.For<ICommandProvideInvoker>();
        _provideInvoker.Invoke(Arg.Any<CommandContext>(), Arg.Any<IServiceProvider>())
            .Returns(_ => new ValueTask<IReadOnlyList<object>>([]));
        foreach (var model in new[] { _from, _to })
        {
            var captured = model;
            _inner.GetFor<Balance>(_unit, (ReadModelKey)captured.Id.Value, Arg.Any<CancellationToken>()).Returns(_ => Gated(captured));
        }
    }

    void Destroy()
    {
        _gate.TrySetResult();
        _scheduler.Dispose();
    }

    async Task<DecisionRead<Balance>> Gated(Balance model)
    {
        _entered.TrySetResult();
        if (_openOnEntry)
        {
            _gate.TrySetResult();
        }
        await _gate.Task;
        var token = for_CommandDecisionReads.DecisionFixtures.Protected(model.Id.Value, model);
        _unit.AddDecisionRead(token);
        _tokens.Add(token);
        return token;
    }

    /// <summary>
    /// Opens the gate once the read has been entered, with every wait bounded.
    /// </summary>
    /// <returns>A task completing when the gate is open.</returns>
    protected async Task ReleaseAfterEntry()
    {
        await _entered.Task.WaitAsync(_bound, TestContext.Current.CancellationToken);
        _gate.SetResult();
    }

    /// <summary>
    /// Records that the caller has sampled whether the pending read completed, so the gate may open.
    /// </summary>
    protected void PendingStateObserved() => _observed.TrySetResult();

    /// <summary>
    /// Opens the gate only once the read has been entered and the caller has recorded its pending-state
    /// observation, so the observation can never see a read the gate already released. Every wait is bounded.
    /// </summary>
    /// <returns>A task completing when the gate is open.</returns>
    protected async Task ReleaseAfterPendingStateObserved()
    {
        await _entered.Task.WaitAsync(_bound, TestContext.Current.CancellationToken);
        await _observed.Task.WaitAsync(_bound, TestContext.Current.CancellationToken);
        _gate.SetResult();
    }
}
