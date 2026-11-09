// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Capabilities;

/// <summary>
/// Explicitly admits verified OAuth bearer credentials as well as PATs to a capability-gated endpoint.
/// The endpoint must still enforce its credential profile after capability admission.
/// </summary>
public sealed class AcceptsOAuthCapability
{
    AcceptsOAuthCapability()
    {
    }

    /// <summary>
    /// Gets the endpoint admission metadata instance.
    /// </summary>
    public static AcceptsOAuthCapability Instance { get; } = new();
}
