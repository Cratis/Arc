// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// An audience the code declares as a type.
/// </summary>
/// <param name="Name">The name of the audience.</param>
/// <param name="Description">What the audience is for, from the documentation of the type.</param>
public record AudienceModel(string Name, string? Description);
