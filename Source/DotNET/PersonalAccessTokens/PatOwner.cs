// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Tenancy;

namespace Cratis.Arc.PersonalAccessTokens;

/// <summary>Verified owner binding required by atomic revocation.</summary>
/// <param name="Resource">Receiving resource.</param>
/// <param name="Environment">Receiving environment.</param>
/// <param name="Person">Verified person.</param>
/// <param name="AccountKey">Verified upstream account.</param>
/// <param name="Tenant">Verified tenant.</param>
internal sealed record PatOwner(string Resource, string Environment, CallerPersonId Person, CallerAccountKey AccountKey, TenantId Tenant);
