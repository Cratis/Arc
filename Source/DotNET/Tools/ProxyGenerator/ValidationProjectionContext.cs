// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator;

/// <summary>The current observation scope, allocated only when reporting is enabled.</summary>
/// <param name="Endpoint">The endpoint being described.</param>
/// <param name="Member">The owning property or parameter.</param>
/// <param name="Source">The source of the observed rules.</param>
/// <param name="Optional">Whether inferred concept presence rules are omitted.</param>
internal sealed record ValidationProjectionContext(ValidationProjectionEndpoint? Endpoint, string? Member = null, ValidationProjectionSource Source = ValidationProjectionSource.Validator, bool Optional = false);
