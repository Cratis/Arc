// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers;

/// <summary>
/// Verified person assurance; never fabricated for a service.
/// </summary>
/// <param name="Idp">The identity provider.</param>
/// <param name="AuthTime">The authentication time in Unix seconds.</param>
/// <param name="Acr">The assurance context.</param>
/// <param name="Amr">Optional authentication methods.</param>
public sealed record CallerAssurance(string Idp, long AuthTime, string? Acr, IReadOnlyList<string>? Amr);
