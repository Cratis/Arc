// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Tenancy;

namespace Cratis.Arc.Identity.Callers;

/// <summary>Bindings supplied only by a verified credential or its credential-id mapping, never request input.</summary>
/// <remarks>Internal until Cratis/Arc#3701 locks; then publish.</remarks>
/// <param name="Id">The checked grant identifier.</param>
/// <param name="AccountKey">The delegating account.</param>
/// <param name="Actor">The current actor.</param>
/// <param name="Resource">The bound resource.</param>
/// <param name="Audience">The bound audience.</param>
/// <param name="Tenant">The bound tenant.</param>
/// <param name="Environment">The bound environment.</param>
internal sealed record CallerGrantBinding(CallerGrantId Id, CallerAccountKey AccountKey, CallerActor Actor, string Resource, string Audience, TenantId Tenant, string Environment);
