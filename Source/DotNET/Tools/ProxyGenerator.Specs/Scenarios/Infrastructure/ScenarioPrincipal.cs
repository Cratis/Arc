// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

/// <summary>
/// Detects needless replacement of an ordinary authenticated HTTP principal by a base-class clone.
/// </summary>
/// <param name="identity">The authenticated identity.</param>
public class ScenarioPrincipal(ClaimsIdentity identity) : ClaimsPrincipal(identity);
