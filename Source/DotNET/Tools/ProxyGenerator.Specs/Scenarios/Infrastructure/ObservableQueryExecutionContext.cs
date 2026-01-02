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
    public async Task UpdateAndWaitAsync(TResult data, TimeSpan? timeout = null)
    {
        // Reset for new update
        _updateReceived = new TaskCompletionSource<bool>();
        var initialCount = Updates.Count;

        // Trigger the update
        updateReceiver(data);

        // Wait for WebSocket notification
        timeout ??= TimeSpan.FromSeconds(5);
        using var cts = new CancellationTokenSource(timeout.Value);
        try
        {
            await _updateReceived.Task.WaitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            throw new JavaScriptProxyExecutionFailed($"No WebSocket update received within {timeout.Value.TotalSeconds} seconds for subscription {SubscriptionId}");
        }

        // Extract new updates from JavaScript
        var currentCount = runtime.Evaluate<int>($"globalThis.__obsSubscriptions['{SubscriptionId}'].updates.length");
        for (var i = initialCount; i < currentCount; i++)
        {
            var updateJson = runtime.Evaluate<string>($"JSON.stringify(globalThis.__obsSubscriptions['{SubscriptionId}'].updates[{i}].data)") ?? "{}";
            var update = System.Text.Json.JsonSerializer.Deserialize<TResult>(updateJson, jsonOptions);
            if (update is not null)
            {
                Updates.Add(update);
            }
        }
    }
}
