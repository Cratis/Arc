// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers;

/// <summary>
/// The current authenticated actor, separate from the delegating person.
/// </summary>
/// <param name="Issuer">The actor issuer.</param>
/// <param name="Subject">The actor subject.</param>
public sealed record CallerActor(string Issuer, string Subject);
