// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;

namespace Samples.Features.ProtectedProfiles.ReadByKey;

/// <summary>
/// Reads a profile through a protected lookup by the key the caller supplies.
/// </summary>
/// <param name="Subject">The subject whose profile is read.</param>
[Command, AllowAnonymous]
public record ReadProfile(string Subject)
{
    /// <summary>
    /// Handles the command by returning the display name of the looked-up profile.
    /// </summary>
    /// <param name="profile">The profile of <see cref="Subject"/>.</param>
    /// <returns>The display name.</returns>
    public string Handle([LookUpBy(nameof(Subject))] Profile profile) => profile.DisplayName;
}
