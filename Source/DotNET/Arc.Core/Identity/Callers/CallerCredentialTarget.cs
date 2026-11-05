// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers;

/// <summary>The resource, audience, and environment asserted by a verified credential, not by the receiving installation.</summary>
/// <remarks>Internal until Cratis/Arc#3701 locks; then publish.</remarks>
/// <param name="Resource">The verified resource indicator.</param>
/// <param name="Audience">The verified audience slug.</param>
/// <param name="Environment">The verified environment.</param>
internal sealed record CallerCredentialTarget(string Resource, string Audience, string Environment);
