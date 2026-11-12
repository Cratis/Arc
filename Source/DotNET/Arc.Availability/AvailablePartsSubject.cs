// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;

namespace Cratis.Arc.Availability;

/// <summary>
/// Represents the subject behind the available parts query. It emits the current decision when observed and again after
/// each change in what the decisions are made from, but only when the decision differs from the last one emitted.
/// </summary>
sealed class AvailablePartsSubject : ISubject<AvailableParts>, IDisposable
{
    readonly IUnavailableParts _parts;
    readonly ReplaySubject<AvailableParts> _subject = new(1);
    readonly CancellationTokenSource _disposed = new();
    readonly IDisposable _subscription;
    readonly SemaphoreSlim _gate = new(1, 1);
    UnavailablePart[]? _last;

    /// <summary>
    /// Initializes a new instance of the <see cref="AvailablePartsSubject"/> class.
    /// </summary>
    /// <param name="parts">The <see cref="IUnavailableParts"/> to observe.</param>
    internal AvailablePartsSubject(IUnavailableParts parts)
    {
        _parts = parts;
        _subscription = parts.OnChanged(() => new ValueTask(Recompute()));
        _ = Recompute();
    }

    /// <inheritdoc/>
    public void OnCompleted() => _subject.OnCompleted();

    /// <inheritdoc/>
    public void OnError(Exception error) => _subject.OnError(error);

    /// <inheritdoc/>
    public void OnNext(AvailableParts value) => _subject.OnNext(value);

    /// <inheritdoc/>
    public IDisposable Subscribe(IObserver<AvailableParts> observer) => _subject.Subscribe(observer);

    /// <inheritdoc/>
    public void Dispose()
    {
        _subscription.Dispose();
        _disposed.Cancel();
        _subject.Dispose();
        _gate.Dispose();
        _disposed.Dispose();
    }

    async Task Recompute()
    {
        try
        {
            await _gate.WaitAsync(_disposed.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        try
        {
            var unavailable = (await _parts.Get(_disposed.Token)).ToArray();
            if (_last?.AsSpan().SequenceEqual(unavailable) != true)
            {
                _last = unavailable;
                _subject.OnNext(new AvailableParts(unavailable));
            }
        }
        catch (OperationCanceledException) when (_disposed.IsCancellationRequested)
        {
            // The subscription ended.
        }
        catch (AvailabilityNotDecidedException ex) when (_last is null)
        {
            // Nothing was known yet: the first decision failed, which is a retryable error for the query.
            _subject.OnError(ex);
        }
        catch (AvailabilityNotDecidedException)
        {
            // A later decision failed: keep the last known decision rather than reporting a denial.
        }
        catch (ObjectDisposedException)
        {
            // The scope of the request ended while the change was announced.
        }
        finally
        {
            _gate.Release();
        }
    }
}
