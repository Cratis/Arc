// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// Marks built-in performers whose legacy verdict is the same built-in evaluator already checked by the async dispatcher.
/// </summary>
internal interface IFrameworkAuthorizationQueryTarget : IAuthorizationQueryTarget
{
    /// <summary>
    /// Gets whether this instance owns a legacy evaluator that differs from the DI evaluator.
    /// </summary>
    bool HasIndependentLegacyVerdict { get; }
}
