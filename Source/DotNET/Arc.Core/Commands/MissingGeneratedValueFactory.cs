// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// The exception that is thrown when a generated parameter has no factory and dynamic code is unavailable.
/// </summary>
/// <param name="commandType">The command declaring the generated value.</param>
/// <param name="name">The generated parameter name.</param>
/// <param name="valueType">The declared value type.</param>
public class MissingGeneratedValueFactory(Type commandType, string name, Type valueType)
    : Exception($"Command '{commandType.FullName}' parameter '{name}' requires a generated value factory for '{valueType.FullName}'. Reference the Arc source generator; reflection generation is unavailable when dynamic code is unsupported.");
