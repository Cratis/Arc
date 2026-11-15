// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Keys;

namespace Samples.Features.ProtectedProfiles.Register;

/// <summary>
/// Registers a profile for a subject. This is how the acceptance specs seed the Chronicle event store through the
/// published application.
/// </summary>
/// <param name="Subject">The subject the profile belongs to; it is the event source.</param>
/// <param name="DisplayName">The name the profile is shown with.</param>
[Command, AllowAnonymous]
public record RegisterProfile([Key] string Subject, string DisplayName)
{
    /// <summary>
    /// Handles the command by stating that the profile was registered.
    /// </summary>
    /// <returns>The <see cref="ProfileRegistered"/> event.</returns>
    public ProfileRegistered Handle() => new(DisplayName);
}
