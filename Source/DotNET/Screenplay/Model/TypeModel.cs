// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents a shape declared at the document level - several values under one name.
/// </summary>
/// <param name="Name">The name of the type.</param>
/// <param name="Properties">The values the type carries.</param>
/// <remarks>
/// A concept is one value with a name; this is the other half of what an artifact can carry. A record an event
/// holds - the lines of an approved timesheet, the milestones of an onboarding - is referred to by name from the
/// property carrying it, and this is the declaration that name resolves to.
/// </remarks>
public record TypeModel(string Name, IEnumerable<PropertyModel> Properties);
