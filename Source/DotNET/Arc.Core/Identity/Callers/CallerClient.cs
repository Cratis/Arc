// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers;

/// <summary>
/// Client metadata, not actor authority; PAT labels are always declared.
/// </summary>
/// <param name="Id">The client metadata identifier.</param>
/// <param name="Authentication">The metadata's authentication provenance.</param>
public sealed record CallerClient(string Id, CallerClientAuthentication Authentication);
