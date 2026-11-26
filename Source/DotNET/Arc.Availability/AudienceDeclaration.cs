// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Audiences;

namespace Cratis.Arc.Availability;

/// <summary>
/// An 'audience' the model declares, with what the model says about it.
/// </summary>
/// <param name="Name">The name of the audience.</param>
/// <param name="Description">What the audience is. Empty when the model does not say.</param>
/// <param name="Source">Where in the model it is declared. Empty when unknown.</param>
public record AudienceDeclaration(AudienceName Name, string Description = "", string Source = "");
