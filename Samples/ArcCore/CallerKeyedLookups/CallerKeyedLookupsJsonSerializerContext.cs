// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using Samples.Features.CallerKeyedLookups.ByClaim;
using Samples.Features.CallerKeyedLookups.BySubject;

namespace Samples.Features.CallerKeyedLookups;

/// <summary>
/// Provides reflection-free metadata for the published caller-keyed lookup refusal commands.
/// </summary>
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(RefuseCallerClaimLookup))]
[JsonSerializable(typeof(RefuseCallerSubjectLookup))]
public partial class CallerKeyedLookupsJsonSerializerContext : JsonSerializerContext;
