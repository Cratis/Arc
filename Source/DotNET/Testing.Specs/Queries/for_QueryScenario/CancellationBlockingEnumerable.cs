// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Testing.for_QueryScenario;

public sealed class CancellationBlockingEnumerable(bool blockEnumeratorCreation) : IAsyncEnumerable<ObservableScenarioReadModel>
{
    public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource Canceled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public int EnumeratorDisposals { get; private set; }

    bool BlockEnumeratorCreation => blockEnumeratorCreation;

    public IAsyncEnumerator<ObservableScenarioReadModel> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        if (blockEnumeratorCreation)
        {
            BlockUntilCanceled(cancellationToken);
        }

        return new Enumerator(this, cancellationToken);
    }

    void BlockUntilCanceled(CancellationToken cancellationToken)
    {
        using var registration = cancellationToken.Register(() => Canceled.TrySetResult());
        Entered.TrySetResult();
        Canceled.Task.WaitAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
    }

    sealed class Enumerator(CancellationBlockingEnumerable owner, CancellationToken cancellationToken) : IAsyncEnumerator<ObservableScenarioReadModel>
    {
        public ObservableScenarioReadModel Current => new("Unused");

        public ValueTask<bool> MoveNextAsync()
        {
            if (!owner.BlockEnumeratorCreation)
            {
                owner.BlockUntilCanceled(cancellationToken);
            }
            cancellationToken.ThrowIfCancellationRequested();

            return ValueTask.FromResult(false);
        }

        public ValueTask DisposeAsync()
        {
            owner.EnumeratorDisposals++;

            return ValueTask.CompletedTask;
        }
    }
}
