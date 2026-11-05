// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers;

/// <summary>An exact issuer and canonical lowercase UUID subject; no issuer normalization.</summary>
/// <remarks>Internal until Cratis/Arc#3701 locks; then publish.</remarks>
/// <param name="Issuer">The exact verified issuer.</param>
/// <param name="Subject">The lowercase UUID subject.</param>
internal sealed record CallerAccountKey(string Issuer, string Subject);
