// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Authorization;

/// <summary>
/// Immutable identity content and original reference for conservative comparison of custom principal types.
/// </summary>
/// <param name="Reference">The original principal.</param>
/// <param name="IsStandard">Whether built-in principal/identity types allow content comparison.</param>
/// <param name="Fingerprint">The complete identity content when captured.</param>
internal record PrincipalSnapshot(ClaimsPrincipal? Reference, bool IsStandard, string Fingerprint);
