// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Cratis.Arc.Identity.Workload;

/// <summary>Source-generated, trim- and AOT-safe serialization of the Kubernetes requests workload verification sends.</summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(TokenReviewRequest))]
internal sealed partial class WorkloadKubernetesJsonContext : JsonSerializerContext;
