// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.Context;

/// <summary>
/// A 'provide' line: a named context value added at one level of the model.
/// </summary>
/// <param name="Name">The name of the value under '$context'.</param>
/// <param name="Provider">Where the value comes from.</param>
/// <param name="Scope">The level it is declared at; everything at or below it inherits it.</param>
/// <param name="Required">Whether the value must exist. A required value that is absent refuses the request.</param>
public record ContextValueDeclaration(string Name, IContextValueProvider Provider, ContextScope Scope, bool Required = true);
