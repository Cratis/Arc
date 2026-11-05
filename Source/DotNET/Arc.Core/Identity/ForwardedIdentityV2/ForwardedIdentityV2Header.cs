// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.ForwardedIdentityV2;

/// <summary>A copied raw header occurrence, not authenticated identity.</summary>
/// <param name="Name">The unnormalized header name.</param>
/// <param name="Value">The unnormalized field value.</param>
internal sealed record ForwardedIdentityV2Header(string Name, string Value);
