// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Capabilities;

/// <summary>
/// Describes client metadata without conferring actor authority.
/// </summary>
/// <param name="Id">The client identifier.</param>
/// <param name="IsAuthenticated">Whether the client metadata was authenticated rather than declared.</param>
public sealed record CapabilityClient(string Id, bool IsAuthenticated);
