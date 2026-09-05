// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity;

/// <summary>
/// Represents a development user.
/// </summary>
/// <param name="MicrosoftIdentity">The Microsoft identity representation of the user.</param>
/// <param name="Details">Optional user details.</param>
public record User(ClientPrincipal MicrosoftIdentity, object Details);
