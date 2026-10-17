// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authorization;

/// <summary>
/// Identifies a resolution whose complete policy set can safely run without a bound resource.
/// </summary>
internal interface IResourceFreePolicyResolution
{
    /// <summary>
    /// Gets whether every resolved requirement ignores the resource.
    /// </summary>
    bool CanEvaluateWithoutResource { get; }
}
