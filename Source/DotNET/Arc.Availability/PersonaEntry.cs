// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Audiences;

namespace Cratis.Arc.Availability;

/// <summary>
/// A persona in the manifest, with the audiences the model gives it. Authoring only: it plays no part in evaluation.
/// </summary>
/// <param name="Name">The name of the persona.</param>
/// <param name="Audiences">The audiences it is given.</param>
public record PersonaEntry(string Name, IReadOnlyList<AudienceName> Audiences);
