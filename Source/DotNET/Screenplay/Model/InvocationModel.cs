// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents a command a reaction hands to the command pipeline.
/// </summary>
/// <param name="CommandName">The name of the command invoked.</param>
/// <param name="Mappings">The mappings from the triggering occurrence onto the properties of the command.</param>
public record InvocationModel(string CommandName, IEnumerable<PropertyMappingModel> Mappings);
