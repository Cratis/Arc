// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.ForwardedIdentityV2;

/// <summary>
/// The exception that is thrown when signed forwarding has no explicitly selected replay store.
/// </summary>
public class ForwardedIdentityNonceStoreNotConfigured() : Exception("Forwarded identity v2 requires an explicitly registered nonce store.");
