// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Cratis.Arc.Chronicle.ReadModels.for_CommandDecisionReads.given;

internal sealed class SingleThreadCommandPump : SynchronizationContext
{
    readonly BlockingCollection<Action> _callbacks = new();

    public override void Post(SendOrPostCallback callback, object? state) => _callbacks.Add(() => callback(state));

    internal static async Task Run(Func<Task> action, Action unblock)
    {
        var pump = new SingleThreadCommandPump();
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            SetSynchronizationContext(pump);
            try
            {
                var pending = action();
                while (!pending.IsCompleted)
                {
                    if (pump._callbacks.TryTake(out var callback, 100)) callback();
                }
                pending.GetAwaiter().GetResult();
                completed.SetResult();
            }
            catch (Exception failure)
            {
                completed.SetException(failure);
            }
            finally
            {
                SetSynchronizationContext(null);
            }
        }) { IsBackground = true };
        thread.Start();
        try
        {
            await completed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }
        finally
        {
            // Also release an old blocking implementation after the bounded failure, so it leaves no live worker.
            unblock();
            if (!thread.Join(TimeSpan.FromSeconds(5))) throw new TimeoutException("The command pump did not stop.");
            pump._callbacks.Dispose();
        }
    }
}
