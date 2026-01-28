// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

/// <summary>
/// Represents the execution context for an observable query through the JavaScript proxy.
/// Provides methods to trigger updates and wait for WebSocket notifications.
/// </summary>
/// <typeparam name="TResult">The type of the data.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="ObservableQueryExecutionContext{TResult}"/> class.
/// </remarks>
/// <param name="result">The initial query result.</param>
/// <param name="updates">All updates received.</param>
/// <param name="runtime">The JavaScript runtime.</param>
/// <param name="subscriptionId">The unique subscription ID.</param>
/// <param name="jsonOptions">The JSON serialization options.</param>
/// <param name="updateReceiver">Action to call when test wants to trigger an update.</param>
public class ObservableQueryExecutionContext<TResult>(
    Queries.QueryResult result,
    List<TResult> updates,
    JavaScriptRuntime runtime,
    string subscriptionId,
    System.Text.Json.JsonSerializerOptions jsonOptions,
    Action<TResult> updateReceiver) : IObservableQueryExecutionContext
{
    TaskCompletionSource<bool> _updateReceived = new();

    /// <summary>
    /// Gets the initial query result.
    /// </summary>
    public Queries.QueryResult Result { get; } = result;

    /// <summary>
    /// Gets all updates received.
    /// </summary>
    public List<TResult> Updates { get; } = updates;

    /// <summary>
    /// Gets the most recent data from updates.
    /// </summary>
    public TResult LatestData => Updates[^1];

    /// <summary>
    /// Gets the unique subscription ID for this observable query.
    /// </summary>
    public string SubscriptionId { get; } = subscriptionId;

    /// <summary>
    /// Signals that a WebSocket update has been received for this subscription.
    /// Called by the bridge when JavaScript notifies of an update.
    /// </summary>
    public void SignalUpdateReceived()
    {
        _updateReceived.TrySetResult(true);
    }

    /// <summary>
    /// Triggers a data update and waits for the WebSocket notification.
    /// </summary>
    /// <param name="data">The data to update.</param>
    /// <param name="timeout">Maximum time to wait for the update.</param>
    /// <returns>An awaitable task.</returns>
    /// <exception cref="JavaScriptProxyExecutionFailed">The exception that is thrown when no update is received within the timeout.</exception>
    public async Task UpdateAndWait(TResult data, TimeSpan? timeout = null)
    {
        // Reset for new update
        _updateReceived = new TaskCompletionSource<bool>();
        var initialCount = Updates.Count;

        // Trigger the update
        updateReceiver(data);

        // Wait for WebSocket notification - kept high to account for slower CI/build server environments
        timeout ??= TimeSpan.FromSeconds(30);
        using var cts = new CancellationTokenSource(timeout.Value);

        // Poll for updates with a short delay to handle race conditions where the update
        // arrives before we start waiting on the TaskCompletionSource
        var start = DateTime.UtcNow;
        var lastCheckedCount = initialCount;
        while (DateTime.UtcNow - start < timeout)
        {
            // Check if JavaScript has received the update
            var currentCount = runtime.Evaluate<int>($"globalThis.__obsSubscriptions['{SubscriptionId}'].updates.length");
            lastCheckedCount = currentCount;
            if (currentCount > initialCount)
            {
                // Update received, extract it
                for (var i = initialCount; i < currentCount; i++)
                {
                    var updateJson = runtime.Evaluate<string>($"JSON.stringify(globalThis.__obsSubscriptions['{SubscriptionId}'].updates[{i}].data)") ?? "{}";
                    var update = System.Text.Json.JsonSerializer.Deserialize<TResult>(updateJson, jsonOptions);
                    if (update is not null)
                    {
                        Updates.Add(update);
                    }
                }

                return;
            }

            // Wait for signal or short timeout
            try
            {
                await _updateReceived.Task.WaitAsync(TimeSpan.FromMilliseconds(100), cts.Token);

                // Signal received, check for updates in next iteration
            }
            catch (TimeoutException)
            {
                // No signal yet, loop will check updates again
            }
            catch (OperationCanceledException)
            {
                // Overall timeout reached
                break;
            }
        }

        // Check WebSocket connection status
        var wsExists = runtime.Evaluate<bool>($"globalThis.__obsSubscriptions?.['{SubscriptionId}'] !== undefined");
        var subscriptionInfo = wsExists
            ? $"Subscription exists with {lastCheckedCount} updates (expected > {initialCount})"
            : "Subscription not found in JavaScript";

        throw new JavaScriptProxyExecutionFailed($"No WebSocket update received within {timeout.Value.TotalSeconds} seconds for subscription {SubscriptionId}. {subscriptionInfo}");
    }
}
