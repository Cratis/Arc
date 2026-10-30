// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Authorization.for_AuthorizationPrincipalIdentity.given;

internal sealed class CustomClaimsIdentity() : ClaimsIdentity([new Claim("custom", "æ<&>😀")], "custom");
