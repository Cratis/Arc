// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Introspection;

/// <summary>
/// Identifies a protected catalog endpoint and its forwarded-header trust setting.
/// </summary>
/// <param name="TrustForwardedIdentityHeaders">Whether identity headers are trusted for this catalog.</param>
internal sealed record ProtectedIntrospectionCatalog(bool TrustForwardedIdentityHeaders);
