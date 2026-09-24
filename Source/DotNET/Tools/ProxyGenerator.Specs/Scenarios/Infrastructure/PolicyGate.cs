// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

/// <summary>
/// Signals when a real host enters a policy and releases its verdict explicitly.
/// </summary>
public class PolicyGate : IDisposable
{
    readonly ConcurrentQueue<(string? User, Guid Scope)> _visits = new();
    TaskCompletionSource _entered = NewSignal();
    TaskCompletionSource _secondEntered = NewSignal();
    TaskCompletionSource _released = NewSignal();
    TaskCompletionSource _exited = NewSignal();
    CancellationTokenSource _serverAbort = new();
    CancellationToken _policyToken;

    /// <summary>Gets the native selected principal while the C admission is pending.</summary>
    public string? SelectedNativePrincipal { get; private set; }

    /// <summary>Gets the unchanged native connection principal during the C admission.</summary>
    public string? UnderlyingNativePrincipal { get; private set; }

    /// <summary>Gets the native selected provider's tenant during the C admission.</summary>
    public string? SelectedNativeTenant { get; private set; }

    /// <summary>Gets the unchanged native connection provider's tenant during the C admission.</summary>
    public string? UnderlyingNativeTenant { get; private set; }

    /// <summary>
    /// Resets the gate before starting the next independent request.
    /// </summary>
    public void Reset()
    {
        _visits.Clear();
        _entered = NewSignal();
        _secondEntered = NewSignal();
        _released = NewSignal();
        _exited = NewSignal();
        _serverAbort.Dispose();
        _serverAbort = new CancellationTokenSource();
        _policyToken = default;
        SelectedNativePrincipal = null;
        UnderlyingNativePrincipal = null;
        SelectedNativeTenant = null;
        UnderlyingNativeTenant = null;
    }

    /// <summary>
    /// Waits for a policy invocation, under a bounded hang timeout.
    /// </summary>
    /// <returns>The entry signal.</returns>
    public Task WaitForEntry() => _entered.Task.WaitAsync(TimeSpan.FromSeconds(10));

    /// <summary>
    /// Waits until two distinct requests have entered the policy.
    /// </summary>
    /// <returns>The entry signal.</returns>
    public Task WaitForTwoEntries() => _secondEntered.Task.WaitAsync(TimeSpan.FromSeconds(10));

    /// <summary>
    /// Waits until the policy invocation has exited, including a canceled invocation.
    /// </summary>
    /// <returns>The exit signal.</returns>
    public Task WaitForExit() => _exited.Task.WaitAsync(TimeSpan.FromSeconds(10));

    /// <summary>
    /// Gets the real host request-abort token controlled by this test's server middleware.
    /// </summary>
    public CancellationToken ServerAbortToken => _serverAbort.Token;

    /// <summary>
    /// Gets whether the policy received the token installed on the actual server request.
    /// </summary>
    public bool PolicyUsesServerAbort => _policyToken == _serverAbort.Token;

    /// <summary>
    /// Gets whether the actual policy cancellation token has been signaled.
    /// </summary>
    public bool PolicyTokenCancelled => _policyToken.IsCancellationRequested;

    /// <summary>
    /// Gets whether the policy already left the gate before the test requested cancellation.
    /// </summary>
    public bool HasExited => _exited.Task.IsCompleted;

    /// <summary>
    /// Records the token the policy receives from the command or query pipeline.
    /// </summary>
    /// <param name="token">The policy cancellation token.</param>
    public void ObservePolicyToken(CancellationToken token) => _policyToken = token;

    /// <summary>
    /// Cancels the token observed by the actual Arc HTTP endpoint and policy.
    /// </summary>
    /// <returns>The cancellation operation.</returns>
    public Task AbortServerRequest() => _serverAbort.CancelAsync();

    /// <summary>
    /// Signals the end of one policy invocation.
    /// </summary>
    public void SignalExit() => _exited.TrySetResult();

    /// <summary>
    /// Gets the principals and scoped service instances observed by the policy.
    /// </summary>
    public IReadOnlyList<(string? User, Guid Scope)> Visits => [.. _visits];

    /// <summary>
    /// Records one caller and policy scope.
    /// </summary>
    /// <param name="user">The authenticated user.</param>
    /// <param name="scope">The scoped service identity.</param>
    public void Record(string? user, Guid scope)
    {
        _visits.Enqueue((user, scope));
        if (_visits.Count >= 2)
        {
            _secondEntered.TrySetResult();
        }
    }

    /// <summary>Captures both native HTTP views during the selected C admission.</summary>
    /// <param name="selectedPrincipal">The Arc operation-local native principal.</param>
    /// <param name="underlyingPrincipal">The unmodified connection principal.</param>
    /// <param name="selectedTenant">The operation-local native provider's tenant.</param>
    /// <param name="underlyingTenant">The unmodified connection provider's tenant.</param>
    public void RecordNative(string? selectedPrincipal, string? underlyingPrincipal, string? selectedTenant, string? underlyingTenant)
    {
        SelectedNativePrincipal = selectedPrincipal;
        UnderlyingNativePrincipal = underlyingPrincipal;
        SelectedNativeTenant = selectedTenant;
        UnderlyingNativeTenant = underlyingTenant;
    }

    /// <summary>
    /// Lets the awaiting policy complete.
    /// </summary>
    public void Release() => _released.TrySetResult();

    /// <summary>
    /// Waits for release after announcing policy entry.
    /// </summary>
    /// <param name="token">The request cancellation token.</param>
    /// <returns>The release task.</returns>
    public async Task Wait(CancellationToken token)
    {
        var release = _released.Task;
        _entered.TrySetResult();
        await release.WaitAsync(token);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _serverAbort.Dispose();
        GC.SuppressFinalize(this);
    }

    static TaskCompletionSource NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
}
