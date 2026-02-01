// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Integration.for_WebSocketObservableQueries.given;

namespace Cratis.Arc.EntityFrameworkCore.Integration.for_WebSocketObservableQueries.when_observing_all_entities;

/// <summary>
/// Specs for WebSocket notification when an entity is deleted.
/// </summary>
[Collection(nameof(WebSocketObservableQueriesCollection))]
public class and_entity_is_deleted : a_running_arc_application_with_observable_queries
{
    int _initialMessageCount;
    bool _receivedDeleteNotification;
    const string EntityToDelete = "EntityToDelete";

    async Task Establish()
    {
        // Seed initial data with multiple entities
        SeedData(
            new IntegrationTestEntity { Id = 1, Name = EntityToDelete, IsActive = true },
            new IntegrationTestEntity { Id = 2, Name = "OtherEntity", IsActive = true });

        // Connect to WebSocket endpoint
        var uri = GetWebSocketUri("all-entities");
        await _webSocketClient.Connect(uri);

        // Wait for initial data (should contain both entities)
        await _webSocketClient.WaitForMessages(1, TimeSpan.FromSeconds(5));
        _initialMessageCount = _webSocketClient.MessageCount;
    }

    async Task Because()
    {
        // Delete entity via scoped DbContext
        DeleteEntity(1);

        // Wait for any new message after the delete operation
        _receivedDeleteNotification = await _webSocketClient.WaitForMessages(
            _initialMessageCount + 1,
            TimeSpan.FromSeconds(5));
    }

    [Fact] void should_receive_initial_data() => _initialMessageCount.ShouldBeGreaterThanOrEqual(1);
    [Fact] void should_receive_notification_after_delete() => _receivedDeleteNotification.ShouldBeTrue();
}
