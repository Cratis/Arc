// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// Identifies built-in performers that can distinguish a captured evaluator from scope-resolved authorization.
/// </summary>
internal interface IFrameworkAuthorizationQueryTarget : IAuthorizationQueryTarget
{
    /// <summary>
    /// Gets whether this instance owns a captured legacy evaluator whose verdict must be used instead of the dispatcher fallback.
    /// </summary>
    bool HasIndependentLegacyVerdict { get; }
}
