// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authorization;

/// <summary>
/// Reports whether every effective requirement is a policy registered to evaluate guests.
/// Unknown host resolutions fail closed by not implementing this contract.
/// </summary>
internal interface IAnonymousPolicyResolution
{
    /// <summary>Gets whether all requirements opted in to guest evaluation.</summary>
    bool EvaluatesAnonymous { get; }
}
