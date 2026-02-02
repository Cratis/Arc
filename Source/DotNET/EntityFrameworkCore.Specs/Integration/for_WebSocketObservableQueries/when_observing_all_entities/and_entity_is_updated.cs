// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.EntityFrameworkCore.Integration.for_WebSocketObservableQueries.given;

#pragma warning disable RCS1141 // Add 'param' element to documentation comment

namespace Cratis.Arc.EntityFrameworkCore.Integration.for_WebSocketObservableQueries.when_observing_all_entities;

/// <summary>
/// Specs for WebSocket notification when an entity is updated.
/// </summary>
[Collection(nameof(WebSocketObservableQueriesCollection))]
public class and_entity_is_updated(ApplicationFixture fixture) : a_running_arc_application_with_observable_queries(fixture)
{
    int _initialMessageCount;
    bool _receivedUpdateNotification;
    const string UpdatedName = "UpdatedEntityName";

    async Task Establish()
    {
        // Seed initial data
        SeedData(new IntegrationTestEntity { Id = 1, Name = "OriginalName", IsActive = true });

        // Connect to WebSocket endpoint
        var uri = GetWebSocketUri("all-entities");
        await _webSocketClient.Connect(uri);

        // Wait for initial data
        await _webSocketClient.WaitForMessages(1, TimeSpan.FromSeconds(5));
        _initialMessageCount = _webSocketClient.MessageCount;
    }

    async Task Because()
    {
        // Update the entity via scoped DbContext
        UpdateEntity(1, entity => entity.Name = UpdatedName);

        // Wait for the notification with the updated name
        _receivedUpdateNotification = await _webSocketClient.WaitForMessage(
            result =>
            {
                var data = result.Data as JsonElement?;
                if (data?.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in data.Value.EnumerateArray())
                    {
                        if (item.TryGetProperty("name", out var nameProp) ||
                            item.TryGetProperty("Name", out nameProp))
                        {
                            if (nameProp.GetString() == UpdatedName)
                            {
                                return true;
                            }
                        }
                    }
                }
                return false;
            },
            TimeSpan.FromSeconds(5));
    }

    [Fact] void should_receive_initial_data() => _initialMessageCount.ShouldBeGreaterThanOrEqual(1);
    [Fact] void should_receive_notification_for_updated_entity() => _receivedUpdateNotification.ShouldBeTrue();
    [Fact] void should_have_more_messages_than_initial() => _webSocketClient.MessageCount.ShouldBeGreaterThan(_initialMessageCount);
}
