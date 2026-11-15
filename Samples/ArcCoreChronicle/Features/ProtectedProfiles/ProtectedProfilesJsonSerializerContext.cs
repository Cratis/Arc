// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using Samples.Features.ProtectedProfiles.ReadByKey;
using Samples.Features.ProtectedProfiles.ReadMine;
using Samples.Features.ProtectedProfiles.Register;

namespace Samples.Features.ProtectedProfiles;

/// <summary>
/// Provides reflection-free metadata for the published protected profile commands.
/// </summary>
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(RegisterProfile))]
[JsonSerializable(typeof(ReadProfile))]
[JsonSerializable(typeof(ReadMyProfile))]
public partial class ProtectedProfilesJsonSerializerContext : JsonSerializerContext;
