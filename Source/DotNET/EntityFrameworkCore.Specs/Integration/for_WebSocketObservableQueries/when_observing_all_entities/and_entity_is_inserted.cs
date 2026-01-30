// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.EntityFrameworkCore.Integration.for_WebSocketObservableQueries.given;

namespace Cratis.Arc.EntityFrameworkCore.Integration.for_WebSocketObservableQueries.when_observing_all_entities;

/// <summary>
/// Specs for WebSocket notification when an entity is inserted.
/// </summary>
public class and_entity_is_inserted : a_running_arc_application_with_observable_queries
{
    bool _receivedInitialData;
    bool _receivedInsertNotification;
    IntegrationTestEntity _insertedEntity;

    async Task Establish()
    {
        // Seed initial data
        SeedData(new IntegrationTestEntity { Id = 1, Name = "Initial", IsActive = true });

        // Connect to WebSocket endpoint
        var uri = GetWebSocketUri("all-entities");
        await _webSocketClient.Connect(uri);

        // Wait for at least one message (initial data)
        _receivedInitialData = await _webSocketClient.WaitForMessages(1, TimeSpan.FromSeconds(5));

        _insertedEntity = new IntegrationTestEntity { Id = 2, Name = "NewEntity", IsActive = true };
    }

    async Task Because()
    {
        // Small delay to ensure the WebSocket subscription is fully established
        await Task.Delay(100);

        // Insert a new entity via scoped DbContext
        InsertEntity(_insertedEntity);

        // Wait for the notification containing the new entity
        _receivedInsertNotification = await _webSocketClient.WaitForMessage(
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
                            if (nameProp.GetString() == "NewEntity")
                            {
                                return true;
                            }
                        }
                    }
                }
                return false;
            },
            TimeSpan.FromSeconds(10));
    }

    [Fact] void should_receive_initial_data() => _receivedInitialData.ShouldBeTrue();
    [Fact] void should_receive_notification_for_inserted_entity() => _receivedInsertNotification.ShouldBeTrue();
}
