// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Cratis.Arc.Commands;
using Cratis.Arc.Identity;
using Cratis.Arc.Introspection;
using Cratis.Arc.Queries;
using Cratis.Arc.Tenancy;

namespace Cratis.Arc;

/// <summary>
/// Source-generated JSON metadata for the wire types Arc owns: the command and query result envelopes, change sets and
/// paging, the observable query hub and WebSocket messages, the observable query requests, and the identity and
/// introspection responses.
/// </summary>
/// <remarks>
/// <para>
/// Only metadata is generated, never a serialization fast path, so the <see cref="System.Text.Json.JsonSerializerOptions"/>
/// it is chained into keep deciding naming, ignore conditions, number handling, encoding and converters - the concept,
/// enum and derived type converters included - exactly as the reflection-based resolver did.
/// </para>
/// <para>
/// Members typed as <see cref="object"/> - <see cref="QueryResult.Data"/>, the <see cref="ChangeSet"/> items, message
/// payloads - carry the application's own types. They are resolved by their runtime type through the rest of the
/// resolver chain, see <see cref="JsonSerializerOptionsConfiguration.ConfigureArcDefaults"/>.
/// </para>
/// </remarks>
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(QueryResult))]
[JsonSerializable(typeof(CommandResult))]
[JsonSerializable(typeof(ChangeSet))]
[JsonSerializable(typeof(PagingInfo))]
[JsonSerializable(typeof(ObservableQueryHubMessage))]
[JsonSerializable(typeof(WebSocketMessage))]
[JsonSerializable(typeof(ObservableQuerySubscriptionRequest))]
[JsonSerializable(typeof(ObservableQuerySSESubscribeRequest))]
[JsonSerializable(typeof(ObservableQuerySSEUnsubscribeRequest))]
[JsonSerializable(typeof(QueryRequestEnvelope))]
[JsonSerializable(typeof(ClientPrincipal))]
[JsonSerializable(typeof(IdentityProviderResult))]
[JsonSerializable(typeof(IEnumerable<User>))]
[JsonSerializable(typeof(IEnumerable<Tenant>))]
[JsonSerializable(typeof(JsonObject))]
[JsonSerializable(typeof(List<CommandIntrospectionMetadata>))]
[JsonSerializable(typeof(List<QueryIntrospectionMetadata>))]
internal sealed partial class ArcJsonSerializerContext : JsonSerializerContext;
