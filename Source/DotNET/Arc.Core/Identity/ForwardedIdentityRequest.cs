// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity;

/// <summary>
/// Holds bounded canonical inputs and a private exact transport snapshot.
/// </summary>
/// <param name="CanonicalBytes">The MAC input.</param>
/// <param name="Headers">The covered, OWS-trimmed headers.</param>
/// <param name="Snapshot">The exact inputs used only for request-local cache comparison.</param>
/// <param name="Method">The method from the same verified host-observed snapshot.</param>
internal sealed record ForwardedIdentityRequest(byte[] CanonicalBytes, IReadOnlyDictionary<string, string> Headers, string Snapshot, string Method);
