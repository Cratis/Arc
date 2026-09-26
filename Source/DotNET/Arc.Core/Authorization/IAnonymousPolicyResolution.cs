// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authorization;

/// <summary>
/// Describes whether every resolved requirement allows evaluation for an unauthenticated caller.
/// </summary>
internal interface IAnonymousPolicyResolution
{
    /// <summary>
    /// Gets whether the entire resolved declaration may be evaluated without authentication.
    /// </summary>
    bool EvaluatesAnonymous { get; }
}
