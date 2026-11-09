// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.Capabilities;

/// <summary>
/// Describes a request the capability gate refused.
/// </summary>
/// <param name="StatusCode">The HTTP status code the gate decided on.</param>
/// <param name="Reason">Why no verified caller was admitted, when the refusal came from caller verification.</param>
public sealed record CapabilityRefusal(int StatusCode, CallerRefusal? Reason);
