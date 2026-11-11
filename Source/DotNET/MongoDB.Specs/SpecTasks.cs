// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.MongoDB;

/// <summary>
/// Helpers for waiting on a task with a deadline in specs.
/// </summary>
public static class SpecTasks
{
    /// <summary>
    /// Waits for a task to complete, however it completes, for at most the given time.
    /// </summary>
    /// <param name="task">The task to wait for.</param>
    /// <param name="timeout">How long to wait.</param>
    /// <returns><see langword="true"/> if the task completed (successfully, faulted or cancelled) in time, <see langword="false"/> if the wait timed out.</returns>
    public static async Task<bool> CompletesWithin(Task task, TimeSpan timeout)
    {
        try
        {
            await task.WaitAsync(timeout);
            return true;
        }
        catch (TimeoutException) when (!task.IsCompleted)
        {
            return false;
        }
        catch (Exception) when (task.IsCompleted)
        {
            // A faulted or cancelled task did complete; the caller asserts on how.
            return true;
        }
    }
}
