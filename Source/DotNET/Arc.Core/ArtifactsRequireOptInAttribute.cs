// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc;

/// <summary>
/// Requires a host to include this assembly through <see cref="ArcOptions.Artifacts"/> and
/// <see cref="ArtifactsOptions.Include"/> before serving its model-bound commands and read-model queries.
/// Unlike <see cref="ExcludeFromDiscoveryAttribute"/>, this removes the endpoints, not only their descriptions.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public sealed class ArtifactsRequireOptInAttribute : Attribute;
