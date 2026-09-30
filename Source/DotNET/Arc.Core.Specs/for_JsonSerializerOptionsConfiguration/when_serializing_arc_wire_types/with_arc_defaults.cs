// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Commands;
using Cratis.Arc.for_JsonSerializerOptionsConfiguration.given;
using Cratis.Arc.Identity;
using Cratis.Arc.Queries;
using Cratis.Arc.Tenancy;

namespace Cratis.Arc.for_JsonSerializerOptionsConfiguration.when_serializing_arc_wire_types;

/// <summary>
/// Arc's own wire types now resolve through its source-generated metadata; what reaches the client must not change by
/// a single byte from when every type was resolved through reflection.
/// </summary>
public class with_arc_defaults : Specification
{
    static readonly ClientPrincipal _principal = new()
    {
        IdentityProvider = "aad",
        UserId = "user-æøå",
        UserDetails = "Ola <ola@example.com>",
        UserRoles = ["Admin"],
        Claims = [new ClientPrincipalClaim { typ = "name", val = "Ola \"Nordmann\"" }],
    };

    JsonSerializerOptions _options;
    JsonSerializerOptions _optionsAsBefore;

    void Establish()
    {
        _options = new JsonSerializerOptions().ConfigureArcDefaults();
        _optionsAsBefore = representative_wire_values.OptionsAsBefore();
    }

    [Fact] void should_serialize_a_query_result_the_same() => ShouldSerializeTheSame(representative_wire_values.QueryResult, typeof(QueryResult));
    [Fact] void should_serialize_a_query_result_by_its_runtime_type_the_same() => ShouldSerializeTheSame(representative_wire_values.QueryResult, typeof(object));
    [Fact] void should_serialize_an_unauthorized_query_result_the_same() => ShouldSerializeTheSame(QueryResult.Unauthorized(representative_wire_values.CorrelationId), typeof(QueryResult));
    [Fact] void should_serialize_a_command_result_the_same() => ShouldSerializeTheSame(representative_wire_values.CommandResult, typeof(CommandResult));
    [Fact] void should_serialize_a_command_result_with_a_response_the_same() => ShouldSerializeTheSame(representative_wire_values.CommandResultWithResponse, typeof(CommandResult<OrderReadModel>));
    [Fact] void should_serialize_a_change_set_the_same() => ShouldSerializeTheSame(representative_wire_values.ChangeSet, typeof(ChangeSet));
    [Fact] void should_serialize_a_query_result_hub_message_the_same() => ShouldSerializeTheSame(ObservableQueryHubMessage.CreateQueryResult("query-1", representative_wire_values.QueryResult, 7), typeof(ObservableQueryHubMessage));
    [Fact] void should_serialize_a_connected_hub_message_the_same() => ShouldSerializeTheSame(ObservableQueryHubMessage.CreateConnected("connection-1", TimeSpan.FromSeconds(15)), typeof(ObservableQueryHubMessage));
    [Fact] void should_serialize_an_error_hub_message_the_same() => ShouldSerializeTheSame(ObservableQueryHubMessage.CreateError("query-1", "Failed <badly>"), typeof(ObservableQueryHubMessage));
    [Fact] void should_serialize_an_unauthorized_hub_message_the_same() => ShouldSerializeTheSame(ObservableQueryHubMessage.CreateUnauthorized("query-1"), typeof(ObservableQueryHubMessage));
    [Fact] void should_serialize_a_pong_hub_message_the_same() => ShouldSerializeTheSame(ObservableQueryHubMessage.CreatePong(1234567890), typeof(ObservableQueryHubMessage));
    [Fact] void should_serialize_a_web_socket_data_message_the_same() => ShouldSerializeTheSame(WebSocketMessage.CreateData(representative_wire_values.QueryResult), typeof(WebSocketMessage));
    [Fact] void should_serialize_a_web_socket_pong_message_the_same() => ShouldSerializeTheSame(WebSocketMessage.Pong(1234567890), typeof(WebSocketMessage));
    [Fact] void should_serialize_a_client_principal_the_same() => ShouldSerializeTheSame(_principal, typeof(ClientPrincipal));
    [Fact] void should_serialize_users_the_same() => ShouldSerializeTheSame(new[] { new User(_principal, representative_wire_values.Order) }, typeof(IEnumerable<User>));
    [Fact] void should_serialize_tenants_the_same() => ShouldSerializeTheSame(new[] { new Tenant(new TenantId("tenant-1"), new TenantName("Tenant <1>")) }, typeof(IEnumerable<Tenant>));

    void ShouldSerializeTheSame(object value, Type type) =>
        JsonSerializer.Serialize(value, type, _options).ShouldEqual(JsonSerializer.Serialize(value, type, _optionsAsBefore));
}
