// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc;

/// <summary>
/// Selects startup safety requirements independently of the hosting environment name.
/// </summary>
public enum DeploymentProfile
{
    /// <summary>Requires verified forwarded identity.</summary>
    Default = 0,

    /// <summary>Allows explicitly configured unsigned forwarding for local development only.</summary>
    LocalDevelopment = 1
}
