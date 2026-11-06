// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.ForwardedIdentityV2;

/// <summary>
/// The exception that is thrown when forwarded identity verification material is not a public P-256 key.
/// </summary>
/// <param name="keyId">The configured key identifier.</param>
/// <param name="reason">The public key requirement that was not satisfied.</param>
public class InvalidForwardedIdentityPublicKey(string keyId, string reason) : Exception($"Invalid forwarded identity public key '{keyId}': {reason}");
