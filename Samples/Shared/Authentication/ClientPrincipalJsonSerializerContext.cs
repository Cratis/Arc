// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;
using Cratis.Arc.Identity;

namespace Samples.Authentication;

/// <summary>
/// Provides web-default JSON metadata for Microsoft client principals.
/// </summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web, GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(ClientPrincipal))]
public partial class ClientPrincipalJsonSerializerContext : JsonSerializerContext;
