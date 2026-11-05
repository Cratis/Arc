// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.ForwardedIdentityV2.given;

internal sealed record Inputs(string Method, string Path, string Aud, long Now, Header[] Headers, string TenantHeader = "x-example-tenant", bool ReplaySensitive = true, bool AuthenticatedTls = true, bool StoreAvailable = true, bool StoreFull = false, string Replica = "replica-a", string RealmIssuer = "https://identity.example.test", VerifiedBearerResult? VerifiedBearer = null);
