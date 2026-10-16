// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Cratis.Arc.Connections;

/// <summary>
/// Represents the source-generated JSON serialization for the HTTP connection resolver contract.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    UseStringEnumConverter = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(HttpConnectionRequest))]
[JsonSerializable(typeof(ResolvedConnection))]
public partial class ConnectionResolutionJsonSerializerContext : JsonSerializerContext;
