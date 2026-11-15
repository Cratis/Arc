// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;

namespace Samples.Features.ProtectedProfiles.ReadMine;

/// <summary>
/// Reads the profile of the caller, keyed by the caller's subject rather than by anything the client sends.
/// </summary>
[Command, AllowAnonymous]
public record ReadMyProfile
{
    /// <summary>
    /// Handles the command by returning the display name of the caller's profile.
    /// </summary>
    /// <param name="profile">The profile of the caller's subject.</param>
    /// <returns>The display name.</returns>
    public string Handle([LookUpBy<CallerSubject>] Profile profile) => profile.DisplayName;
}
