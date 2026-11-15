// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

namespace Samples.Features.ProtectedProfiles;

/// <summary>
/// The event that occurs when a profile has been registered for a subject.
/// </summary>
/// <param name="DisplayName">The name the profile is shown with.</param>
[EventType]
public record ProfileRegistered(string DisplayName);

/// <summary>
/// Represents the profile of one subject. The read model is keyed by the subject.
/// </summary>
/// <param name="Id">The subject the profile belongs to.</param>
/// <param name="DisplayName">The name the profile is shown with.</param>
[ReadModel]
[FromEvent<ProfileRegistered>]
public record Profile([Key] string Id, string DisplayName);
