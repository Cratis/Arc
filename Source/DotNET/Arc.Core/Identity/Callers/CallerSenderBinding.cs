// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers;

/// <summary>
/// A successfully checked sender binding. Absence is represented by null, not by an unchecked proof.
/// </summary>
/// <param name="KeyThumbprint">The matching verified key.</param>
/// <param name="Jti">The replay-checked identifier.</param>
public sealed record CallerSenderBinding(string KeyThumbprint, string Jti);
