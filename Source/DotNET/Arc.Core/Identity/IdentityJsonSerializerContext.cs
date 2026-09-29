// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Cratis.Arc.Identity;

/// <summary>
/// Source-generated JSON metadata for the identity types Arc owns.
/// </summary>
/// <remarks>
/// Only metadata is generated, never a serialization fast path, so the options it is chained into keep deciding
/// naming, ignore conditions, encoding and converters - the concept converters included - exactly as the
/// reflection-based resolver did. The <see cref="IdentityProviderResult.Details"/> are the application's own type,
/// known only at runtime, and are resolved by the rest of the resolver chain.
/// </remarks>
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(IdentityProviderResult))]
internal sealed partial class IdentityJsonSerializerContext : JsonSerializerContext;
