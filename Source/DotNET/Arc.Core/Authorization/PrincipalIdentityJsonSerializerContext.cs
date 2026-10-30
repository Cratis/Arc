// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Cratis.Arc.Authorization;

/// <summary>
/// Provides principal fingerprint metadata without changing the default JSON format or encoder.
/// </summary>
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(IEnumerable<PrincipalIdentityContent>), TypeInfoPropertyName = "Identities")]
internal partial class PrincipalIdentityJsonSerializerContext : JsonSerializerContext;
