// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.Specs.CommandResponseHandlerDependency;

/// <summary>
/// Represents an event with an attribute declared on its base type.
/// </summary>
/// <param name="Name">The project name.</param>
public record InheritedProjectRegistered(string Name) : ProjectRegistered(Name);
