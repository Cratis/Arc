// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authorization;

/// <summary>
/// Explicitly permits anonymous evaluation of an ASP.NET Core-registered policy by name.
/// </summary>
/// <param name="Name">The ASP.NET Core policy name.</param>
public record AnonymousAspNetAuthorizationPolicyRegistration(string Name);
