// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Arc.ProxyGenerator.Specs.CommandResponseHandlerDependency;

/// <summary>
/// A concept validated by a derived validator in an indirectly Core-referencing assembly.
/// </summary>
/// <param name="Value">The name value.</param>
public record TransitiveName(string Value) : ConceptAs<string>(Value);
